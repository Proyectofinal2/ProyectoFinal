using System.Globalization;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Catalogo;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

/// <summary>
/// Servicio para consumir los endpoints de gestión de usuarios de la API.
/// HU-AUT-005: crear cuentas.
/// HU-AUT-006: listar, desactivar, reactivar cuentas.
/// El Bearer token JWT se agrega automáticamente a todas las solicitudes por AuthTokenHandler.
/// </summary>
public class UsuariosApiService(HttpClient httpClient, ILogger<UsuariosApiService> logger) 
    : IUsuariosApiService
{
    /// <summary>
    /// Crea una nueva cuenta de usuario en la API.
    /// Retorna (success, message, userResponse).
    /// </summary>
    public async Task<(bool Success, string Message, UserApiResponse? Data)>
        CrearUsuarioAsync(NuevaCuentaViewModel modelo)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("usuarios", modelo);

            var resultado = await ApiResponseHelper.ParseAsync<UserApiResponse>(
                response,
                "Usuario creado exitosamente.",
                "Error al crear el usuario. Intenta de nuevo.");

            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado al crear usuario: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", null);
        }
    }

    /// <summary>
    /// Obtiene todas las cuentas de usuario con filtros opcionales.
    /// Retorna (success, message, listado).
    /// </summary>
    public async Task<(bool Success, string Message, List<UserApiResponse>? Data)>
        ObtenerTodosAsync(string? busqueda, FiltroEstadoCuenta estado)
    {
        try
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                queryParams.Add($"busqueda={Uri.EscapeDataString(busqueda)}");
            }

            if (estado == FiltroEstadoCuenta.Activas)
            {
                queryParams.Add("soloActivos=true");
            }
            else if (estado == FiltroEstadoCuenta.Inactivas)
            {
                queryParams.Add("soloActivos=false");
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await httpClient.GetAsync($"usuarios{queryString}");

            var resultado = await ApiResponseHelper.ParseAsync<List<UserApiResponse>>(
                response,
                "Usuarios obtenidos exitosamente.",
                "Error al obtener el listado de usuarios. Intenta de nuevo.");

            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado al obtener usuarios: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", null);
        }
    }

    /// <summary>
    /// Desactiva una cuenta de usuario (la marca como inactiva).
    /// Retorna (success, message, userResponse).
    /// </summary>
    public async Task<(bool Success, string Message, UserApiResponse? Data)>
        DesactivarAsync(int id, string? motivo = null)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"usuarios/{id}/desactivar", new { motivo });

            var resultado = await ApiResponseHelper.ParseAsync<UserApiResponse>(
                response,
                "La cuenta fue desactivada exitosamente.",
                "Error al desactivar la cuenta. Intenta de nuevo.");

            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado al desactivar usuario: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", null);
        }
    }

    /// <summary>
    /// Reactiva una cuenta de usuario (la marca como activa).
    /// Retorna (success, message, userResponse).
    /// </summary>
    public async Task<(bool Success, string Message, UserApiResponse? Data)>
        ReactivarAsync(int id, string? motivo = null)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"usuarios/{id}/reactivar", new { motivo });

            var resultado = await ApiResponseHelper.ParseAsync<UserApiResponse>(
                response,
                "La cuenta fue reactivada exitosamente.",
                "Error al reactivar la cuenta. Intenta de nuevo.");

            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado al reactivar usuario: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", null);
        }
    }

    // ------------------------------------------------------------
    // HU-AUT-007 — Editar cuentas e historial
    // ------------------------------------------------------------

    /// <summary>Obtiene una cuenta por id (para el formulario de edicion).</summary>
    public Task<(bool Success, string Message, UserApiResponse? Data)> ObtenerPorIdAsync(int id) =>
        EjecutarAsync<UserApiResponse>(
            () => httpClient.GetAsync($"usuarios/{id}"),
            "Cuenta obtenida.", "No se pudo obtener la cuenta. Intenta de nuevo.");

    /// <summary>Edita nombre, usuario, correos y rol de una cuenta.</summary>
    public Task<(bool Success, string Message, UserApiResponse? Data)> ActualizarAsync(int id, EditarCuentaViewModel modelo) =>
        EjecutarAsync<UserApiResponse>(
            () => httpClient.PutAsJsonAsync($"usuarios/{id}", new
            {
                modelo.NombreCompleto,
                modelo.CorreoElectronico,
                modelo.CorreoPersonal,
                modelo.NombreUsuario,
                modelo.Rol
            }),
            "Cuenta actualizada.", "No se pudo actualizar la cuenta. Intenta de nuevo.");

    /// <summary>Una pagina del historial de cuentas (10 por pagina, la fija la API).</summary>
    public Task<(bool Success, string Message, PaginaApiResponse<HistorialUsuarioApiResponse>? Data)> ObtenerHistorialAsync(
        string? busqueda, int? idUsuario, string? accion, int pagina)
    {
        var consulta = new List<string> { $"pagina={pagina.ToString(CultureInfo.InvariantCulture)}" };
        if (!string.IsNullOrWhiteSpace(busqueda)) consulta.Add($"busqueda={Uri.EscapeDataString(busqueda.Trim())}");
        if (idUsuario.HasValue) consulta.Add($"idUsuario={idUsuario.Value}");
        if (!string.IsNullOrEmpty(accion)) consulta.Add($"accion={Uri.EscapeDataString(accion)}");

        return EjecutarAsync<PaginaApiResponse<HistorialUsuarioApiResponse>>(
            () => httpClient.GetAsync("historial-usuarios?" + string.Join("&", consulta)),
            "Historial obtenido.", "No se pudo obtener el historial. Intenta de nuevo.");
    }

    /// <summary>Administradores que hicieron algun cambio (filtro «Realizado por»).</summary>
    public Task<(bool Success, string Message, List<UsuarioHistorialApiResponse>? Data)> ObtenerResponsablesHistorialAsync() =>
        EjecutarAsync<List<UsuarioHistorialApiResponse>>(
            () => httpClient.GetAsync("historial-usuarios/usuarios"),
            "Usuarios obtenidos.", "No se pudieron obtener los usuarios. Intenta de nuevo.");

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
            logger.LogError($"Error inesperado en cuentas: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", default);
        }
    }
}
