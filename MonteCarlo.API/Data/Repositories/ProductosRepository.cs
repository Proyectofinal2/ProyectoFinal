using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class ProductosRepository(MonteCarloDbContext context) : IProductosRepository
{
    public Task<List<Producto>> ObtenerTodosAsync(string? busqueda, int? idCategoria, bool? disponible)
    {
        var consulta = context.Productos.AsNoTracking().Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(p => p.Nombre.Contains(texto));
        }

        if (idCategoria.HasValue)
            consulta = consulta.Where(p => p.IdCategoria == idCategoria.Value);

        if (disponible.HasValue)
            consulta = consulta.Where(p => p.Disponible == disponible.Value);

        return consulta
            .OrderBy(p => p.Categoria.Orden).ThenBy(p => p.IdCategoria).ThenBy(p => p.Nombre)
            .ToListAsync();
    }

    public Task<Producto?> ObtenerPorIdAsync(int idProducto) =>
        context.Productos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.IdProducto == idProducto);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirIdProducto) =>
        context.Productos.AnyAsync(p => p.Nombre == nombre && (!excluirIdProducto.HasValue || p.IdProducto != excluirIdProducto.Value));

    public Task<List<Producto>> ObtenerDisponiblesParaVentaAsync() =>
        context.Productos.AsNoTracking().Include(p => p.Categoria)
            .Where(p => p.Disponible && p.Categoria.Activa)
            .OrderBy(p => p.Categoria.Orden).ThenBy(p => p.Nombre)
            .ToListAsync();

    public Task<bool> TieneVentasAsync(int idProducto) =>
        context.DetallesVenta.AnyAsync(d => d.IdProducto == idProducto);

    public async Task<HashSet<int>> ObtenerIdsConVentasAsync() =>
        (await context.DetallesVenta.AsNoTracking().Select(d => d.IdProducto).Distinct().ToListAsync()).ToHashSet();

    public Task<Producto?> ObtenerParaEliminarAsync(int idProducto) =>
        context.Productos.Include(p => p.Categoria).Include(p => p.Historial)
            .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

    public void Eliminar(Producto producto) => context.Productos.Remove(producto);

    public void Agregar(Producto producto) => context.Productos.Add(producto);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
