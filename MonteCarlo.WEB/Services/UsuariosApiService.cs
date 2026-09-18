using MonteCarlo.WEB.Models.Autenticacion;
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

            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<UserApiResponse>(
                response,
                "Usuario creado exitosamente.",
                "Error al crear el usuario. Intenta de nuevo.");

            return (success, message, data);
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

            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<List<UserApiResponse>>(
                response,
                "Usuarios obtenidos exitosamente.",
                "Error al obtener el listado de usuarios. Intenta de nuevo.");

            return (success, message, data);
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
        DesactivarAsync(int id)
    {
        try
        {
            var response = await httpClient.PostAsync($"usuarios/{id}/desactivar", null);

            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<UserApiResponse>(
                response,
                "La cuenta fue desactivada exitosamente.",
                "Error al desactivar la cuenta. Intenta de nuevo.");

            return (success, message, data);
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
        ReactivarAsync(int id)
    {
        try
        {
            var response = await httpClient.PostAsync($"usuarios/{id}/reactivar", null);

            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<UserApiResponse>(
                response,
                "La cuenta fue reactivada exitosamente.",
                "Error al reactivar la cuenta. Intenta de nuevo.");

            return (success, message, data);
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
}
