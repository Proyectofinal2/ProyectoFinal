using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class HistorialCatalogoRepository(MonteCarloDbContext context) : IHistorialCatalogoRepository
{
    public void Agregar(HistorialCatalogo entrada) => context.HistorialCatalogo.Add(entrada);

    public async Task<(List<HistorialCatalogo> Items, int Total)> BuscarAsync(
        string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina, int tamano)
    {
        // Cascada de categoria: una sola fila (la de la categoria) en lugar de una por producto.
        var consulta = Consultar().Where(h => h.Motivo == null ||
            (!h.Motivo.StartsWith(HistorialCatalogoMotivos.PorDeshabilitarCategoria) &&
             !h.Motivo.StartsWith(HistorialCatalogoMotivos.PorHabilitarCategoria)));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(h =>
                (h.Producto != null && h.Producto.Nombre.Contains(texto)) ||
                (h.Categoria != null && h.Categoria.Nombre.Contains(texto)) ||
                (h.NombreElemento != null && h.NombreElemento.Contains(texto)));
        }

        if (!string.IsNullOrEmpty(tipo))
            consulta = consulta.Where(h => h.TipoElemento == tipo);

        if (idUsuario.HasValue)
            consulta = consulta.Where(h => h.IdUsuario == idUsuario.Value);

        if (!string.IsNullOrEmpty(accion))
            consulta = consulta.Where(h => h.Accion == accion);

        var total = await consulta.CountAsync();

        var items = await consulta
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.IdHistorialCatalogo)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync();

        return (items, total);
    }

    public Task<List<HistorialCatalogo>> ObtenerPorProductoAsync(int idProducto, int maximo) =>
        Consultar()
            .Where(h => h.IdProducto == idProducto)
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.IdHistorialCatalogo)
            .Take(maximo)
            .ToListAsync();

    public Task<List<Usuario>> ObtenerUsuariosAsync() =>
        context.HistorialCatalogo.AsNoTracking()
            .Select(h => h.Usuario).Distinct()
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();

    private IQueryable<HistorialCatalogo> Consultar() =>
        context.HistorialCatalogo.AsNoTracking()
            .Include(h => h.Usuario)
            .Include(h => h.Producto).ThenInclude(p => p!.Categoria)
            .Include(h => h.Categoria);
}
