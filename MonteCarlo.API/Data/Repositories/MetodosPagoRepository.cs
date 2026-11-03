using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class MetodosPagoRepository(MonteCarloDbContext context) : IMetodosPagoRepository
{
    public Task<List<MetodoPago>> ObtenerActivosAsync() =>
        context.MetodosPago.AsNoTracking().Where(m => m.Activo).OrderBy(m => m.IdMetodoPago).ToListAsync();

    public Task<MetodoPago?> ObtenerPorIdAsync(int idMetodoPago) =>
        context.MetodosPago.AsNoTracking().FirstOrDefaultAsync(m => m.IdMetodoPago == idMetodoPago);
}
