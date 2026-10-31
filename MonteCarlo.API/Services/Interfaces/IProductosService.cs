using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IProductosService
{
    Task<Result<List<ProductoResponse>>> ObtenerTodosAsync(string? busqueda, int? idCategoria, bool? disponible);
    Task<Result<ProductoResponse>> ObtenerPorIdAsync(int idProducto);

    /// HU-CON-001, escenario 1: registra el producto como disponible.
    Task<Result<ProductoResponse>> CrearAsync(GuardarProductoRequest request, int idUsuario);

    /// HU-CON-001, escenario 2: actualiza nombre, precio y/o categoria; el historial guarda una fila por campo cambiado.
    Task<Result<ProductoResponse>> ActualizarAsync(int idProducto, GuardarProductoRequest request, int idUsuario);

    /// HU-CON-001, escenario 3: lo retira del selector de consumo; no se elimina, el historial de ventas se conserva.
    Task<Result<ProductoResponse>> DeshabilitarAsync(int idProducto, int idUsuario);

    /// Elimina el producto solo si no tiene ventas asociadas (409 si las tiene). Su historial se conserva con el nombre guardado.
    Task<Result<ProductoResponse>> EliminarAsync(int idProducto, int idUsuario);

    /// Si la categoria esta deshabilitada exige <paramref name="habilitarCategoria"/>: habilita el producto y la categoria.
    Task<Result<ProductoResponse>> HabilitarAsync(int idProducto, bool habilitarCategoria, int idUsuario);
}
