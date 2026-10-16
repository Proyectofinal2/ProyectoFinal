using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class HistorialCierresRepository(MonteCarloDbContext context) : IHistorialCierresRepository
{
    private const int MaximoResultados = 200;

    public Task<List<HistorialCierre>> BuscarAsync(string? tipo, string? accion, DateOnly? desde, DateOnly? hasta)
    {
        var consulta = context.HistorialCierres.AsNoTracking().Include(h => h.Usuario).AsQueryable();

        if (!string.IsNullOrEmpty(tipo))
            consulta = consulta.Where(h => h.TipoCierre == tipo);

        if (!string.IsNullOrEmpty(accion))
            consulta = consulta.Where(h => h.Accion == accion);

        if (desde.HasValue)
        {
            var inicio = desde.Value.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(h => h.Fecha >= inicio);
        }

        if (hasta.HasValue)
        {
            var fin = hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(h => h.Fecha < fin);
        }

        return consulta.OrderByDescending(h => h.Fecha).ThenByDescending(h => h.IdHistorialCierre)
            .Take(MaximoResultados).ToListAsync();
    }
}
