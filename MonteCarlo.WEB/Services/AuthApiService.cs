using System.Net;
using System.Text.Json;
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
        var resultado = await EnviarAsync<LoginApiResponse>(
            () => httpClient.PostAsJsonAsync("auth/login", new LoginApiRequest
            {
                Usuario = usuario,
                Contrasena = contrasena
            }),
            "Login exitoso",
            "Error de autenticación. Intenta de nuevo.");

        int? segundosBloqueoRestantes = resultado.ErrorData?.TryGetProperty("segundosRestantes", out var prop) == true
            ? prop.GetInt32()
            : null;

        return (resultado.Success, resultado.Message, resultado.Data, segundosBloqueoRestantes);
    }

    /// <summary>
    /// Solicita a la API el envío del enlace de recuperación de contraseña. HU-AUT-003.
    /// </summary>
    public async Task<(bool Success, string Message)> RecuperarContrasenaAsync(string usuarioOCorreo)
    {
        var resultado = await EnviarAsync<object>(
            () => httpClient.PostAsJsonAsync("auth/recuperar-contrasena", new { usuarioOCorreo }),
            "Si el usuario existe, se enviará un correo con instrucciones para restablecer la contraseña.",
            "No se pudo procesar la solicitud. Intenta nuevamente más tarde.");

        return (resultado.Success, resultado.Message);
    }

    /// <summary>
    /// Restablece la contraseña usando el token recibido por correo. HU-AUT-003.
    /// </summary>
    public async Task<(bool Success, string Message, bool TokenInvalido)> RestablecerContrasenaAsync(string token, string nuevaContrasena)
    {
        var resultado = await EnviarAsync<object>(
            () => httpClient.PostAsJsonAsync("auth/restablecer-contrasena", new { token, nuevaContrasena }),
            "Contraseña actualizada correctamente.",
            MensajeTokenInvalido);

        // ConfigureApiBehaviorOptions (Program.cs de la API) tambien devuelve 400 cuando
        // ModelState es invalido (ej. NuevaContrasena no cumple StringLength), y ese caso
        // llega con Errors poblado, a diferencia del 400 por token invalido/expirado.
        var tokenInvalido = resultado.StatusCode == HttpStatusCode.BadRequest
            && (resultado.Errors == null || resultado.Errors.Count == 0);

        return (resultado.Success, resultado.Message, tokenInvalido);
    }

    /// <summary>
    /// Verifica si un token de recuperación es válido, sin consumirlo. HU-AUT-003:
    /// permite avisarle al usuario de un enlace roto antes de llenar el formulario.
    /// </summary>
    public async Task<bool> ValidarTokenRecuperacionAsync(string token)
    {
        var resultado = await EnviarAsync<object>(
            () => httpClient.GetAsync($"auth/validar-token-recuperacion?token={Uri.EscapeDataString(token)}"),
            "Token válido.",
            MensajeTokenInvalido);

        return resultado.Success;
    }

    private const string MensajeTokenInvalido = "El enlace de recuperación no es válido o ha expirado.";

    /// <summary>
    /// Envía una solicitud a la API y parsea el envelope de respuesta genérico. Centraliza
    /// el manejo de errores de conexión que comparten todos los métodos de este servicio.
    /// </summary>
    private async Task<(bool Success, string Message, T? Data, JsonElement? ErrorData, HttpStatusCode? StatusCode, Dictionary<string, string[]>? Errors)> EnviarAsync<T>(
        Func<Task<HttpResponseMessage>> enviar, string successMessage, string errorMessage)
    {
        try
        {
            var response = await enviar();

            var (success, message, data, errorData, errors) = await ApiResponseHelper.ParseAsync<T>(
                response, successMessage, errorMessage);

            return (success, message, data, errorData, response.StatusCode, errors);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", default, null, null, null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado al llamar a la API: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", default, null, null, null);
        }
    }
}
