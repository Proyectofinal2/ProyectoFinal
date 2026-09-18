using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>
/// Controlador de autenticación.
/// Maneja el login de administradores.
/// HU-AUT-001: Inicio de sesión seguro.
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
}
