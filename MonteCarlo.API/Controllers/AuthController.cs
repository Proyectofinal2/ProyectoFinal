using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>
/// Controlador de autenticación.
/// Maneja el login de administradores y la recuperación de contraseña.
/// HU-AUT-001: Inicio de sesión seguro. HU-AUT-003: Recuperación de contraseña.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{

    /// <summary>
    /// Endpoint de login.
    /// Autentica un administrador y retorna un token JWT.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode,
                ApiResponse.ErrorResponse(result.Message, result.ErrorData));
        }

        return StatusCode(result.StatusCode,
            ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    /// <summary>
    /// Solicita la recuperación de contraseña de un administrador.
    /// Envía un correo con un enlace para restablecerla si la cuenta existe y está activa.
    /// Siempre responde con un mensaje genérico, sin revelar si la cuenta existe.
    /// </summary>
    [HttpPost("recuperar-contrasena")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await authService.SolicitarRecuperacionAsync(request);
        return result.Success
                    ? StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, null))
                    : StatusCode(result.StatusCode, ApiResponse.ErrorResponse(result.Message, null));
    }

    /// <summary>
    /// Restablece la contraseña de un administrador usando el token recibido por correo.
    /// </summary>
    [HttpPost("restablecer-contrasena")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await authService.RestablecerContrasenaAsync(request);
        return result.Success
                    ? StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, null))
                    : StatusCode(result.StatusCode, ApiResponse.ErrorResponse(result.Message, null));
    }

    /// <summary>
    /// Verifica si un token de recuperación de contraseña es válido, sin consumirlo.
    /// Permite a la UI avisar de un enlace roto antes de llenar el formulario. HU-AUT-003.
    /// </summary>
    [HttpGet("validar-token-recuperacion")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidarTokenRecuperacion([FromQuery] string? token)
    {
        var result = await authService.ValidarTokenRecuperacionAsync(token);
        return result.Success
                    ? StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, null))
                    : StatusCode(result.StatusCode, ApiResponse.ErrorResponse(result.Message, null));
    }
}
