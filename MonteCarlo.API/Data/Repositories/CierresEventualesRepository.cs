using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class CierresEventualesRepository(MonteCarloDbContext context) : ICierresEventualesRepository
{
    public Task<List<CierreEventual>> ObtenerDesdeAsync(DateOnly desde) =>
        context.CierresEventuales.Include(c => c.UsuarioCreacion)
            .Where(c => c.Fecha >= desde).OrderBy(c => c.Fecha).ToListAsync();

    public Task<CierreEventual?> ObtenerPorIdAsync(int id) =>
        context.CierresEventuales.Include(c => c.UsuarioCreacion)
            .FirstOrDefaultAsync(c => c.IdCierreEventual == id);

    public Task<bool> ExistePorFechaAsync(DateOnly fecha) =>
        context.CierresEventuales.AnyAsync(c => c.Fecha == fecha);

    public void Agregar(CierreEventual cierre) => context.CierresEventuales.Add(cierre);

    public void AgregarHistorial(HistorialCierre historial) => context.HistorialCierres.Add(historial);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
