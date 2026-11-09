using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Data.Repositories;

public class VentasRepository(MonteCarloDbContext context) : IVentasRepository
{
    public Task<Venta?> ObtenerPorReservaAsync(int idReserva) =>
        context.Ventas
            .Include(v => v.Pedidos).ThenInclude(p => p.Usuario)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.IdReserva == idReserva);

    public Task<bool> ExisteParaReservaAsync(int idReserva) =>
        context.Ventas.AsNoTracking().AnyAsync(v => v.IdReserva == idReserva);

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

    public async Task<Dictionary<int, VentaReservaResumen>> ObtenerResumenVentasPorReservaAsync(IReadOnlyCollection<int> idsReserva)
    {
        if (idsReserva.Count == 0)
            return [];

        var datos = await context.Ventas
            .AsNoTracking()
            .Where(v => v.IdReserva != null && idsReserva.Contains(v.IdReserva.Value))
            .Select(v => new
            {
                IdReserva = v.IdReserva!.Value,
                v.IdVenta,
                v.Estado,
                v.Total,
                Productos = v.Detalles.Where(d => !d.Anulado).Sum(d => (int?)d.Cantidad) ?? 0
            })
            .ToListAsync();

        return datos.ToDictionary(
            d => d.IdReserva,
            d => new VentaReservaResumen(d.IdReserva, d.IdVenta, d.Estado, d.Total, d.Productos));
    }

    public async Task<Dictionary<int, DateTime>> ObtenerMesasConCuentaAbiertaAsync()
    {
        var filas = await context.ReservasMesa
            .AsNoTracking()
            .Where(rm => rm.Reserva.EstadoReserva.Nombre == "Confirmada"
                         && rm.Reserva.Ventas.Any(v => v.Estado == VentaEstados.Abierta))
            .Select(rm => new
            {
                rm.IdMesa,
                rm.Reserva.FechaAtencion,
                FechaVenta = rm.Reserva.Ventas.Where(v => v.Estado == VentaEstados.Abierta).Select(v => v.FechaVenta).First()
            })
            .ToListAsync();

        // Una mesa con dos cuentas abiertas (no deberia pasar) se toma por la mas reciente.
        return filas
            .GroupBy(f => f.IdMesa)
            .ToDictionary(g => g.Key, g => g.Max(f => f.FechaAtencion ?? f.FechaVenta));
    }

    public void Agregar(Venta venta) => context.Ventas.Add(venta);

    public void MarcarModificada(Venta venta) =>
        context.Entry(venta).Property(v => v.Total).IsModified = true;

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();

    public async Task<List<Venta>> ObtenerConFiltrosAsync(FiltroVentasRequest filtro)
    {
        var query = context.Ventas
            .Include(v => v.MetodoPago)
            .AsQueryable();

        // 1. Rango de fechas (si no mandan nada, usa el día actual)
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var inicio = filtro.FechaInicio ?? hoy;
        var fin = filtro.FechaFin ?? inicio;

        var inicioDateTime = inicio.ToDateTime(TimeOnly.MinValue);
        var finDateTime = fin.ToDateTime(TimeOnly.MaxValue);

        query = query.Where(v => v.FechaVenta >= inicioDateTime && v.FechaVenta <= finDateTime);

        // 2. Filtros adicionales
        if (!string.IsNullOrWhiteSpace(filtro.TipoVenta))
            query = query.Where(v => v.TipoVenta == filtro.TipoVenta);

        if (filtro.IdMetodoPago.HasValue)
            query = query.Where(v => v.IdMetodoPago == filtro.IdMetodoPago.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
            query = query.Where(v => v.Estado == filtro.Estado);

        return await query.OrderBy(v => v.FechaVenta).ToListAsync();
    }

    public async Task<ResumenVentasDashboardDatos> ObtenerResumenDashboardAsync(DateTime inicio, DateTime finExclusivo)
    {
        var cobradas = context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado == VentaEstados.Cobrada
                        && v.FechaCobro.HasValue
                        && v.FechaCobro.Value >= inicio
                        && v.FechaCobro.Value < finExclusivo);

        var resumen = await cobradas
            .GroupBy(_ => 1)
            .Select(g => new { Ingresos = g.Sum(v => v.Total), Cantidad = g.Count() })
            .FirstOrDefaultAsync();

        // Las ventas cobradas con importe positivo tienen metodo por restriccion de BD. Se conserva
        // tambien un metodo hoy inactivo, porque la relacion historica sigue existiendo.
        var metodoPredominante = await cobradas
            .Where(v => v.IdMetodoPago.HasValue)
            .GroupBy(v => new { v.IdMetodoPago, Nombre = v.MetodoPago!.Nombre })
            .Select(g => new { g.Key.Nombre, IdMetodoPago = g.Key.IdMetodoPago!.Value, Ingresos = g.Sum(v => v.Total) })
            .OrderByDescending(x => x.Ingresos)
            .ThenBy(x => x.Nombre)
            .ThenBy(x => x.IdMetodoPago)
            .FirstOrDefaultAsync();

        return new ResumenVentasDashboardDatos(
            resumen?.Ingresos ?? 0m,
            resumen?.Cantidad ?? 0,
            metodoPredominante?.Nombre,
            metodoPredominante?.Ingresos);
    }

    public Task<Venta?> ObtenerDetallePorIdAsync(int idVenta) =>
    context.Ventas
        .Include(v => v.MetodoPago)
        .Include(v => v.UsuarioRegistro)
        .Include(v => v.Detalles).ThenInclude(d => d.Producto)
        .FirstOrDefaultAsync(v => v.IdVenta == idVenta);
}
