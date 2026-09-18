using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

/// <summary>
/// Servicio para consumir los endpoints de autenticación de la API.
/// Maneja login y guarda el token JWT para requests posteriores.
/// </summary>
public class AuthApiService(HttpClient httpClient, ILogger<AuthApiService> logger)
    : IAuthApiService
{
    /// <summary>
    /// Intenta autenticar un usuario contra la API.
    /// Retorna (success, message, loginResponse).
    /// </summary>
    public async Task<(bool Success, string Message, LoginApiResponse? Data, int? SegundosBloqueoRestantes)>
        LoginAsync(string usuario, string contrasena)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("auth/login", new LoginApiRequest
            {
                Usuario = usuario,
                Contrasena = contrasena
            });

            var (success, message, data, errorData) = await ApiResponseHelper.ParseAsync<LoginApiResponse>(
                response,
                "Login exitoso",
                "Error de autenticación. Intenta de nuevo.");

            int? segundosBloqueoRestantes = errorData?.TryGetProperty("segundosRestantes", out var prop) == true
                ? prop.GetInt32()
                : null;

            return (success, message, data, segundosBloqueoRestantes);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", null, null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado en login: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", null, null);
        }
    }
}
