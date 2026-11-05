using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class MetodosPagoRepository(MonteCarloDbContext context) : IMetodosPagoRepository
{
    public Task<List<MetodoPago>> ObtenerActivosAsync() =>
        context.MetodosPago.AsNoTracking().Where(m => m.Activo).OrderBy(m => m.IdMetodoPago).ToListAsync();

    // Aquí NO usamos AsNoTracking para poder guardar el cambio de estado
    public Task<MetodoPago?> ObtenerPorIdAsync(int idMetodoPago) =>
        context.MetodosPago.FirstOrDefaultAsync(m => m.IdMetodoPago == idMetodoPago);

    public Task<List<MetodoPago>> ObtenerTodosAsync() =>
        context.MetodosPago.OrderBy(m => m.IdMetodoPago).ToListAsync();

    public Task<bool> ExisteNombreAsync(string nombre) =>
        context.MetodosPago.AnyAsync(m => m.Nombre.ToLower() == nombre.ToLower());

    public void Agregar(MetodoPago metodo) => context.MetodosPago.Add(metodo);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}