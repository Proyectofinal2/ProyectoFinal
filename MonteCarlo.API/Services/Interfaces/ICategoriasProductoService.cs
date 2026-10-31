using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface ICategoriasProductoService
{
    /// En el orden del menu, con el total de productos y cuantos estan disponibles.
    Task<Result<List<CategoriaProductoResponse>>> ObtenerTodasAsync();

    Task<Result<CategoriaProductoResponse>> CrearAsync(GuardarCategoriaProductoRequest request, int idUsuario);
    Task<Result<CategoriaProductoResponse>> RenombrarAsync(int idCategoria, GuardarCategoriaProductoRequest request, int idUsuario);

    /// <paramref name="direccion"/>: -1 sube una posicion, +1 la baja.
    Task<Result<List<CategoriaProductoResponse>>> MoverAsync(int idCategoria, int direccion, int idUsuario);

    /// Deshabilita la categoria y todos sus productos disponibles.
    Task<Result<CambioEstadoCategoriaResponse>> DeshabilitarAsync(int idCategoria, int idUsuario);

    /// Elimina la categoria solo si no tiene productos. Su historial se conserva con el nombre guardado.
    Task<Result<CategoriaProductoResponse>> EliminarAsync(int idCategoria, int idUsuario);

    /// Habilita la categoria y todos sus productos (incluso los que se deshabilitaron por separado).
    Task<Result<CambioEstadoCategoriaResponse>> HabilitarAsync(int idCategoria, int idUsuario);
}
