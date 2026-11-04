using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class VentasRepository(MonteCarloDbContext context) : IVentasRepository
{
    public Task<Venta?> ObtenerPorReservaAsync(int idReserva) =>
        context.Ventas
            .Include(v => v.Pedidos).ThenInclude(p => p.Usuario)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.IdReserva == idReserva);

    public Task<PedidoVenta?> ObtenerPedidoPorClaveAsync(Guid clave) =>
        context.PedidosVenta
            .AsNoTracking()
            .Include(p => p.Venta)
            .FirstOrDefaultAsync(p => p.ClaveIdempotencia == clave);

    public Task<DetalleVenta?> ObtenerLineaAsync(int idDetalle) =>
        context.DetallesVenta
            .Include(d => d.Venta).ThenInclude(v => v.Detalles)
            .FirstOrDefaultAsync(d => d.IdDetalleVenta == idDetalle);

    public Task<bool> TieneCuentaAbiertaAsync(int idReserva) =>
        context.Ventas.AnyAsync(v => v.IdReserva == idReserva && v.Estado == VentaEstados.Abierta);

    public async Task<Dictionary<int, CuentaAbiertaResumen>> ObtenerResumenCuentasAbiertasAsync(IReadOnlyCollection<int> idsReserva)
    {
        if (idsReserva.Count == 0)
            return [];

        var datos = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado == VentaEstados.Abierta && v.IdReserva != null && idsReserva.Contains(v.IdReserva.Value))
            .Select(v => new
            {
                IdReserva = v.IdReserva!.Value,
                v.IdVenta,
                v.Total,
                v.RowVersion,
                Productos = v.Detalles.Where(d => !d.Anulado).Sum(d => (int?)d.Cantidad) ?? 0
            })
            .ToListAsync();

        return datos.ToDictionary(
            d => d.IdReserva,
            d => new CuentaAbiertaResumen(d.IdReserva, d.IdVenta, d.Total, d.Productos, Convert.ToBase64String(d.RowVersion)));
    }

    public async Task<HashSet<int>> ObtenerIdsMesaConCuentaAbiertaAsync() =>
        (await context.ReservasMesa
            .AsNoTracking()
            .Where(rm => rm.Reserva.EstadoReserva.Nombre == "Confirmada"
                         && rm.Reserva.Ventas.Any(v => v.Estado == VentaEstados.Abierta))
            .Select(rm => rm.IdMesa)
            .ToListAsync()).ToHashSet();

    public void Agregar(Venta venta) => context.Ventas.Add(venta);

    public void MarcarModificada(Venta venta) =>
        context.Entry(venta).Property(v => v.Total).IsModified = true;

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
