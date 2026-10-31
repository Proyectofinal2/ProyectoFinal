using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class CategoriasProductoRepository(MonteCarloDbContext context) : ICategoriasProductoRepository
{
    public Task<List<CategoriaProducto>> ObtenerTodasAsync() =>
        context.CategoriasProducto.AsNoTracking().Include(c => c.Productos)
            .OrderBy(c => c.Orden).ThenBy(c => c.IdCategoria)
            .ToListAsync();

    public Task<List<CategoriaProducto>> ObtenerTodasParaReordenarAsync() =>
        context.CategoriasProducto
            .OrderBy(c => c.Orden).ThenBy(c => c.IdCategoria)
            .ToListAsync();

    public Task<CategoriaProducto?> ObtenerPorIdAsync(int idCategoria, bool conProductos = false)
    {
        var consulta = context.CategoriasProducto.AsQueryable();
        if (conProductos)
            consulta = consulta.Include(c => c.Productos);

        return consulta.FirstOrDefaultAsync(c => c.IdCategoria == idCategoria);
    }

    public Task<CategoriaProducto?> ObtenerParaEliminarAsync(int idCategoria) =>
        context.CategoriasProducto
            .Include(c => c.Productos)
            .Include(c => c.Historial)
            .FirstOrDefaultAsync(c => c.IdCategoria == idCategoria);

    public void Eliminar(CategoriaProducto categoria) => context.CategoriasProducto.Remove(categoria);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirIdCategoria) =>
        context.CategoriasProducto.AnyAsync(c => c.Nombre == nombre && (!excluirIdCategoria.HasValue || c.IdCategoria != excluirIdCategoria.Value));

    public async Task<int> ObtenerSiguienteOrdenAsync() =>
        (await context.CategoriasProducto.MaxAsync(c => (int?)c.Orden) ?? 0) + 1;

    public void Agregar(CategoriaProducto categoria) => context.CategoriasProducto.Add(categoria);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
