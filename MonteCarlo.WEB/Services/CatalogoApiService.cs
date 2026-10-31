using System.Globalization;
using MonteCarlo.WEB.Models.Catalogo;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

/// <summary>
/// Consume ProductosController, CategoriasProductoController e HistorialCatalogoController de la API (HU-CON-001).
/// El Bearer token lo agrega AuthTokenHandler.
/// </summary>
public class CatalogoApiService(HttpClient httpClient, ILogger<CatalogoApiService> logger) : ICatalogoApiService
{
    // ------------------------------------------------------------
    // Productos
    // ------------------------------------------------------------

    public Task<(bool Success, string Message, List<ProductoApiResponse>? Data)> ObtenerProductosAsync(
        string? busqueda, int? idCategoria, bool? disponible)
    {
        var consulta = new List<string>();
        if (!string.IsNullOrWhiteSpace(busqueda)) consulta.Add($"busqueda={Uri.EscapeDataString(busqueda.Trim())}");
        if (idCategoria.HasValue) consulta.Add($"idCategoria={idCategoria.Value}");
        if (disponible.HasValue) consulta.Add($"disponible={(disponible.Value ? "true" : "false")}");

        return EjecutarAsync<List<ProductoApiResponse>>(
            () => httpClient.GetAsync("productos" + ArmarConsulta(consulta)),
            "Productos obtenidos.", "No se pudieron obtener los productos. Intenta de nuevo.");
    }

    public Task<(bool Success, string Message, ProductoApiResponse? Data)> ObtenerProductoAsync(int idProducto) =>
        EjecutarAsync<ProductoApiResponse>(
            () => httpClient.GetAsync($"productos/{idProducto}"),
            "Producto obtenido.", "No se pudo obtener el producto. Intenta de nuevo.");

    public Task<(bool Success, string Message, ProductoApiResponse? Data)> CrearProductoAsync(
        string nombre, decimal precio, int idCategoria) =>
        EjecutarAsync<ProductoApiResponse>(
            () => httpClient.PostAsJsonAsync("productos", new { nombre, precio, idCategoria }),
            "El producto fue registrado.", "No se pudo registrar el producto. Intenta de nuevo.");

    public Task<(bool Success, string Message, ProductoApiResponse? Data)> ActualizarProductoAsync(
        int idProducto, string nombre, decimal precio, int idCategoria) =>
        EjecutarAsync<ProductoApiResponse>(
            () => httpClient.PutAsJsonAsync($"productos/{idProducto}", new { nombre, precio, idCategoria }),
            "El producto fue actualizado.", "No se pudo actualizar el producto. Intenta de nuevo.");

    public async Task<(bool Success, string Message)> EliminarProductoAsync(int idProducto)
    {
        var (success, message, _) = await EjecutarAsync<ProductoApiResponse>(
            () => httpClient.DeleteAsync($"productos/{idProducto}"),
            "El producto fue eliminado.", "No se pudo eliminar el producto. Intenta de nuevo.");
        return (success, message);
    }

    public Task<(bool Success, string Message, ProductoApiResponse? Data)> DeshabilitarProductoAsync(int idProducto) =>
        EjecutarAsync<ProductoApiResponse>(
            () => httpClient.PostAsync($"productos/{idProducto}/deshabilitar", null),
            "El producto fue deshabilitado.", "No se pudo deshabilitar el producto. Intenta de nuevo.");

    public Task<(bool Success, string Message, ProductoApiResponse? Data)> HabilitarProductoAsync(
        int idProducto, bool habilitarCategoria) =>
        EjecutarAsync<ProductoApiResponse>(
            () => httpClient.PostAsync($"productos/{idProducto}/habilitar?habilitarCategoria={(habilitarCategoria ? "true" : "false")}", null),
            "El producto fue habilitado.", "No se pudo habilitar el producto. Intenta de nuevo.");

    public Task<(bool Success, string Message, List<HistorialCatalogoApiResponse>? Data)> ObtenerHistorialProductoAsync(int idProducto) =>
        EjecutarAsync<List<HistorialCatalogoApiResponse>>(
            () => httpClient.GetAsync($"productos/{idProducto}/historial"),
            "Historial obtenido.", "No se pudo obtener el historial. Intenta de nuevo.");

    // ------------------------------------------------------------
    // Categorias
    // ------------------------------------------------------------

    public Task<(bool Success, string Message, List<CategoriaProductoApiResponse>? Data)> ObtenerCategoriasAsync() =>
        EjecutarAsync<List<CategoriaProductoApiResponse>>(
            () => httpClient.GetAsync("categorias-producto"),
            "Categorías obtenidas.", "No se pudieron obtener las categorías. Intenta de nuevo.");

    public Task<(bool Success, string Message, CategoriaProductoApiResponse? Data)> CrearCategoriaAsync(string nombre) =>
        EjecutarAsync<CategoriaProductoApiResponse>(
            () => httpClient.PostAsJsonAsync("categorias-producto", new { nombre }),
            "La categoría fue creada.", "No se pudo crear la categoría. Intenta de nuevo.");

    public Task<(bool Success, string Message, CategoriaProductoApiResponse? Data)> RenombrarCategoriaAsync(int idCategoria, string nombre) =>
        EjecutarAsync<CategoriaProductoApiResponse>(
            () => httpClient.PutAsJsonAsync($"categorias-producto/{idCategoria}", new { nombre }),
            "La categoría fue renombrada.", "No se pudo renombrar la categoría. Intenta de nuevo.");

    public async Task<(bool Success, string Message)> SubirCategoriaAsync(int idCategoria)
    {
        var (success, message, _) = await EjecutarAsync<List<CategoriaProductoApiResponse>>(
            () => httpClient.PostAsync($"categorias-producto/{idCategoria}/subir", null),
            "La categoría subió una posición.", "No se pudo reordenar la categoría. Intenta de nuevo.");
        return (success, message);
    }

    public async Task<(bool Success, string Message)> BajarCategoriaAsync(int idCategoria)
    {
        var (success, message, _) = await EjecutarAsync<List<CategoriaProductoApiResponse>>(
            () => httpClient.PostAsync($"categorias-producto/{idCategoria}/bajar", null),
            "La categoría bajó una posición.", "No se pudo reordenar la categoría. Intenta de nuevo.");
        return (success, message);
    }

    public Task<(bool Success, string Message, CambioEstadoCategoriaApiResponse? Data)> DeshabilitarCategoriaAsync(int idCategoria) =>
        EjecutarAsync<CambioEstadoCategoriaApiResponse>(
            () => httpClient.PostAsync($"categorias-producto/{idCategoria}/deshabilitar", null),
            "La categoría fue deshabilitada.", "No se pudo deshabilitar la categoría. Intenta de nuevo.");

    public async Task<(bool Success, string Message)> EliminarCategoriaAsync(int idCategoria)
    {
        var (success, message, _) = await EjecutarAsync<CategoriaProductoApiResponse>(
            () => httpClient.DeleteAsync($"categorias-producto/{idCategoria}"),
            "La categoría fue eliminada.", "No se pudo eliminar la categoría. Intenta de nuevo.");
        return (success, message);
    }

    public Task<(bool Success, string Message, CambioEstadoCategoriaApiResponse? Data)> HabilitarCategoriaAsync(int idCategoria) =>
        EjecutarAsync<CambioEstadoCategoriaApiResponse>(
            () => httpClient.PostAsync($"categorias-producto/{idCategoria}/habilitar", null),
            "La categoría fue habilitada.", "No se pudo habilitar la categoría. Intenta de nuevo.");

    // ------------------------------------------------------------
    // Historial
    // ------------------------------------------------------------

    public Task<(bool Success, string Message, PaginaApiResponse<HistorialCatalogoApiResponse>? Data)> ObtenerHistorialAsync(
        string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina)
    {
        var consulta = new List<string> { $"pagina={pagina.ToString(CultureInfo.InvariantCulture)}" };
        if (!string.IsNullOrWhiteSpace(busqueda)) consulta.Add($"busqueda={Uri.EscapeDataString(busqueda.Trim())}");
        if (!string.IsNullOrEmpty(tipo)) consulta.Add($"tipo={Uri.EscapeDataString(tipo)}");
        if (idUsuario.HasValue) consulta.Add($"idUsuario={idUsuario.Value}");
        if (!string.IsNullOrEmpty(accion)) consulta.Add($"accion={Uri.EscapeDataString(accion)}");

        return EjecutarAsync<PaginaApiResponse<HistorialCatalogoApiResponse>>(
            () => httpClient.GetAsync("historial-catalogo" + ArmarConsulta(consulta)),
            "Historial obtenido.", "No se pudo obtener el historial. Intenta de nuevo.");
    }

    public Task<(bool Success, string Message, List<UsuarioHistorialApiResponse>? Data)> ObtenerUsuariosHistorialAsync() =>
        EjecutarAsync<List<UsuarioHistorialApiResponse>>(
            () => httpClient.GetAsync("historial-catalogo/usuarios"),
            "Usuarios obtenidos.", "No se pudieron obtener los usuarios. Intenta de nuevo.");

    // ------------------------------------------------------------
    // Soporte
    // ------------------------------------------------------------

    private static string ArmarConsulta(List<string> partes) => partes.Count == 0 ? "" : "?" + string.Join("&", partes);

    private async Task<(bool Success, string Message, T? Data)> EjecutarAsync<T>(
        Func<Task<HttpResponseMessage>> llamada, string mensajeExito, string mensajeError)
    {
        try
        {
            var response = await llamada();
            var resultado = await ApiResponseHelper.ParseAsync<T>(response, mensajeExito, mensajeError);
            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", default);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado en el catálogo: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", default);
        }
    }
}
