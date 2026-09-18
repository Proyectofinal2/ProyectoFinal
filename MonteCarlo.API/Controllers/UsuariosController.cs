using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>
/// Controlador de gestión de usuarios.
/// HU-AUT-005: crear cuentas de administrador.
/// HU-AUT-006: ver, desactivar y reactivar cuentas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsuariosController(IUsuariosService usuariosService) : ControllerBase
{
    /// <summary>Id de la cuenta autenticada (claim del token JWT).</summary>
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// HU-AUT-005: crea una nueva cuenta de administrador.
    /// Solo el rol "General" puede crear cuentas.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> CrearUsuario([FromBody] CreateUserRequest request)
    {
        var result = await usuariosService.CreateUserAsync(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, ApiResponse.ErrorResponse(result.Message));
        }

        return StatusCode(result.StatusCode,
            ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    /// <summary>
    /// HU-AUT-006, escenario 1: obtiene el listado de cuentas, con filtros opcionales.
    /// Solo el rol "General" puede consultar el listado.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> ObtenerUsuarios([FromQuery] string? busqueda, [FromQuery] bool? soloActivos)
    {
        var result = await usuariosService.ObtenerTodosAsync(busqueda, soloActivos);

        return StatusCode(result.StatusCode,
            ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    /// <summary>
    /// HU-AUT-006, escenario 2: desactiva una cuenta. No se elimina, se conserva para auditoría.
    /// Solo el rol "General" puede desactivar cuentas, y no puede desactivar la propia.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var result = await usuariosService.CambiarEstadoAsync(id, activo: false, IdUsuarioAutenticado);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, ApiResponse.ErrorResponse(result.Message));
        }

        return StatusCode(result.StatusCode,
            ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    /// <summary>
    /// HU-AUT-006, escenario 3: reactiva una cuenta previamente desactivada.
    /// Solo el rol "General" puede reactivar cuentas.
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> Reactivar(int id)
    {
        var result = await usuariosService.CambiarEstadoAsync(id, activo: true, IdUsuarioAutenticado);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, ApiResponse.ErrorResponse(result.Message));
        }

        return StatusCode(result.StatusCode,
            ApiResponse.SuccessResponse(result.Message, result.Data));
    }
}
