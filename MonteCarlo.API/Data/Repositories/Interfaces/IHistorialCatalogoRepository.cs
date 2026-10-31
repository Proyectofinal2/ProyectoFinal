using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IHistorialCatalogoRepository
{
    /// Solo agrega al contexto: se guarda junto con el cambio que lo origina (misma transaccion).
    void Agregar(HistorialCatalogo entrada);

    /// Del mas reciente al mas antiguo. Cada filtro es opcional; la busqueda compara el nombre del producto o de la categoria.
    Task<(List<HistorialCatalogo> Items, int Total)> BuscarAsync(
        string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina, int tamano);

    Task<List<HistorialCatalogo>> ObtenerPorProductoAsync(int idProducto, int maximo);

    /// Administradores que aparecen en el historial.
    Task<List<Usuario>> ObtenerUsuariosAsync();
}
