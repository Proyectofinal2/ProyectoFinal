using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IProductosRepository
{
    /// Productos con su categoria, en el orden del menu. Todos los filtros son opcionales.
    Task<List<Producto>> ObtenerTodosAsync(string? busqueda, int? idCategoria, bool? disponible);

    /// Con seguimiento y con su categoria, para poder modificarlo.
    Task<Producto?> ObtenerPorIdAsync(int idProducto);

    /// Compara sin distinguir mayusculas (segun la coleccion de la base).
    Task<bool> ExisteNombreAsync(string nombre, int? excluirIdProducto);

    /// Lo que puede ofrecer el selector de consumo y venta: producto disponible Y categoria activa.
    Task<List<Producto>> ObtenerDisponiblesParaVentaAsync();

    /// true si el producto aparece en alguna linea de venta o consumo (DetalleVenta).
    Task<bool> TieneVentasAsync(int idProducto);

    /// Ids de los productos que aparecen en alguna venta, para marcar cuales no se pueden eliminar.
    Task<HashSet<int>> ObtenerIdsConVentasAsync();

    /// Con seguimiento, con su categoria y su historial (que se conserva al eliminarlo).
    Task<Producto?> ObtenerParaEliminarAsync(int idProducto);

    void Eliminar(Producto producto);

    void Agregar(Producto producto);
    Task GuardarCambiosAsync();
}
