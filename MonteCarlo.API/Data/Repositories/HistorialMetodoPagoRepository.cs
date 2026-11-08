using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class HistorialMetodoPagoRepository(MonteCarloDbContext context) : IHistorialMetodoPagoRepository
{
    public void Agregar(HistorialMetodoPago entrada) => context.HistorialMetodosPago.Add(entrada);

    public async Task<(List<HistorialMetodoPago> Items, int Total)> BuscarAsync(
        string? busqueda, int? idMetodoPago, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina, int tamano)
    {
        var consulta = context.HistorialMetodosPago.AsNoTracking()
            .Include(h => h.Usuario)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            consulta = consulta.Where(h => h.NombreNuevo.Contains(busqueda));

        if (idMetodoPago.HasValue)
            consulta = consulta.Where(h => h.IdMetodoPago == idMetodoPago.Value);

        if (idUsuario.HasValue)
            consulta = consulta.Where(h => h.IdUsuario == idUsuario.Value);

        if (!string.IsNullOrEmpty(accion))
            consulta = consulta.Where(h => h.Accion == accion);

        if (desde.HasValue)
        {
            var inicio = desde.Value.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(h => h.Fecha >= inicio);
        }

        if (hasta.HasValue)
        {
            var finExclusivo = hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(h => h.Fecha < finExclusivo);
        }

        var total = await consulta.CountAsync();
        var items = await consulta
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.IdHistorialMetodoPago)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync();

        return (items, total);
    }

    public Task<List<Usuario>> ObtenerUsuariosAsync() =>
        context.HistorialMetodosPago.AsNoTracking()
            .Select(h => h.Usuario).Distinct()
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();
}
