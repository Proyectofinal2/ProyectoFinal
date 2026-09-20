using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class CierresFijosRepository(MonteCarloDbContext context) : ICierresFijosRepository
{
    public Task<List<CierreFijo>> ObtenerTodosAsync() =>
        context.CierresFijos.OrderBy(c => c.DiaSemana).ToListAsync();

    public async Task GuardarCambiosAsync(
        IEnumerable<CierreFijo> agregar,
        IEnumerable<CierreFijo> eliminar,
        IEnumerable<HistorialCierre> historial)
    {
        context.CierresFijos.AddRange(agregar);
        context.CierresFijos.RemoveRange(eliminar);
        context.HistorialCierres.AddRange(historial);
        await context.SaveChangesAsync();
    }
}
