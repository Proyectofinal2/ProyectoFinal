using MonteCarlo.WEB.Models.Catalogo;

namespace MonteCarlo.WEB.Services.Interfaces;

/// <summary>Cliente de la API del catalogo de productos, categorias e historial (HU-CON-001).</summary>
public interface ICatalogoApiService
{
    // Productos
    Task<(bool Success, string Message, List<ProductoApiResponse>? Data)> ObtenerProductosAsync(
        string? busqueda, int? idCategoria, bool? disponible);

    Task<(bool Success, string Message, ProductoApiResponse? Data)> ObtenerProductoAsync(int idProducto);

    Task<(bool Success, string Message, ProductoApiResponse? Data)> CrearProductoAsync(
        string nombre, decimal precio, int idCategoria);

    Task<(bool Success, string Message, ProductoApiResponse? Data)> ActualizarProductoAsync(
        int idProducto, string nombre, decimal precio, int idCategoria);

    Task<(bool Success, string Message, ProductoApiResponse? Data)> DeshabilitarProductoAsync(int idProducto);

    /// <summary>Elimina el producto; la API responde 409 si tiene ventas asociadas.</summary>
    Task<(bool Success, string Message)> EliminarProductoAsync(int idProducto);

    /// <param name="habilitarCategoria">Confirma habilitar tambien la categoria si estaba deshabilitada.</param>
    Task<(bool Success, string Message, ProductoApiResponse? Data)> HabilitarProductoAsync(
        int idProducto, bool habilitarCategoria);

    Task<(bool Success, string Message, List<HistorialCatalogoApiResponse>? Data)> ObtenerHistorialProductoAsync(int idProducto);

    // Categorias
    Task<(bool Success, string Message, List<CategoriaProductoApiResponse>? Data)> ObtenerCategoriasAsync();

    Task<(bool Success, string Message, CategoriaProductoApiResponse? Data)> CrearCategoriaAsync(string nombre);

    Task<(bool Success, string Message, CategoriaProductoApiResponse? Data)> RenombrarCategoriaAsync(int idCategoria, string nombre);

    Task<(bool Success, string Message)> SubirCategoriaAsync(int idCategoria);

    Task<(bool Success, string Message)> BajarCategoriaAsync(int idCategoria);

    Task<(bool Success, string Message, CambioEstadoCategoriaApiResponse? Data)> DeshabilitarCategoriaAsync(int idCategoria);

    Task<(bool Success, string Message, CambioEstadoCategoriaApiResponse? Data)> HabilitarCategoriaAsync(int idCategoria);

    /// <summary>Elimina la categoria; la API responde 409 si tiene productos.</summary>
    Task<(bool Success, string Message)> EliminarCategoriaAsync(int idCategoria);

    // Historial (solo Administrador General)
    Task<(bool Success, string Message, PaginaApiResponse<HistorialCatalogoApiResponse>? Data)> ObtenerHistorialAsync(
        string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina);

    Task<(bool Success, string Message, List<UsuarioHistorialApiResponse>? Data)> ObtenerUsuariosHistorialAsync();
}
