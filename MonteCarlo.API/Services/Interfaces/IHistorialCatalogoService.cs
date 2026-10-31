using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IHistorialCatalogoService
{
    /// Tamano fijo de pagina del historial general.
    const int TamanoPagina = 10;

    /// Historial general de productos y categorias, del mas reciente al mas antiguo.
    /// <paramref name="tamano"/> nunca supera <see cref="TamanoPagina"/>.
    Task<Result<PaginaResponse<HistorialCatalogoResponse>>> ObtenerAsync(
        string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina, int tamano);

    /// Ultimos cambios de un producto (para el panel de edicion).
    Task<Result<List<HistorialCatalogoResponse>>> ObtenerPorProductoAsync(int idProducto);

    Task<Result<List<UsuarioHistorialResponse>>> ObtenerUsuariosAsync();
}
