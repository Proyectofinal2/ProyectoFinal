using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface ICategoriasProductoRepository
{
    /// Solo lectura, en el orden del menu, con sus productos para calcular los conteos.
    Task<List<CategoriaProducto>> ObtenerTodasAsync();

    /// Con seguimiento y sin productos, en el orden del menu: para reordenar.
    Task<List<CategoriaProducto>> ObtenerTodasParaReordenarAsync();

    /// Con seguimiento; <paramref name="conProductos"/> trae tambien sus productos (para habilitar o deshabilitar en cascada).
    Task<CategoriaProducto?> ObtenerPorIdAsync(int idCategoria, bool conProductos = false);

    /// Con seguimiento, con sus productos y su historial (el historial se conserva al eliminarla).
    Task<CategoriaProducto?> ObtenerParaEliminarAsync(int idCategoria);

    void Eliminar(CategoriaProducto categoria);

    Task<bool> ExisteNombreAsync(string nombre, int? excluirIdCategoria);
    Task<int> ObtenerSiguienteOrdenAsync();

    void Agregar(CategoriaProducto categoria);
    Task GuardarCambiosAsync();
}
