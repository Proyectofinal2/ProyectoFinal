using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class HistorialHorarioOperacionRepository(MonteCarloDbContext context) : IHistorialHorarioOperacionRepository
{
    public void Agregar(HistorialHorarioOperacion entrada) => context.HistorialHorariosOperacion.Add(entrada);

    public async Task<(List<HistorialHorarioOperacion> Items, int Total)> BuscarAsync(
        byte? diaSemana, int? idUsuario, string? accion, DateOnly? desde, DateOnly? hasta,
        int pagina, int tamano)
    {
        var consulta = context.HistorialHorariosOperacion.AsNoTracking()
            .Include(h => h.Usuario)
            .AsQueryable();

        if (diaSemana.HasValue)
            consulta = consulta.Where(h => h.DiaSemana == diaSemana.Value);

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
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.IdHistorialHorarioOperacion)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync();

        return (items, total);
    }

    public Task<List<Usuario>> ObtenerUsuariosAsync() =>
        context.HistorialHorariosOperacion.AsNoTracking()
            .Select(h => h.Usuario).Distinct()
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();
}
