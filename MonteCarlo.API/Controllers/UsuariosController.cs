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
/// HU-AUT-007: editar cuentas; cada cambio queda en el historial con el administrador responsable.
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
    public async Task<IActionResult> CrearUsuario([FromBody] CreateUserRequest request) =>
        this.Responder(await usuariosService.CreateUserAsync(request, IdUsuarioAutenticado));

    /// <summary>
    /// HU-AUT-006, escenario 1: obtiene el listado de cuentas, con filtros opcionales.
    /// Solo el rol "General" puede consultar el listado.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> ObtenerUsuarios([FromQuery] string? busqueda, [FromQuery] bool? soloActivos) =>
        this.Responder(await usuariosService.ObtenerTodosAsync(busqueda, soloActivos));

    /// <summary>HU-AUT-007: obtiene una cuenta por id (para el formulario de edición).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "General")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerUsuario(int id) =>
        this.Responder(await usuariosService.ObtenerPorIdAsync(id));

    /// <summary>
    /// HU-AUT-007: edita nombre, usuario, correos y rol de una cuenta. Cada campo cambiado queda en el historial
    /// con el administrador que lo hizo. No puede cambiar su propio rol ni quitar el rol a la última cuenta General activa.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "General")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] UpdateUserRequest request) =>
        this.Responder(await usuariosService.ActualizarAsync(id, request, IdUsuarioAutenticado));

    /// <summary>
    /// HU-AUT-006, escenario 2: desactiva una cuenta. No se elimina, se conserva para auditoría.
    /// Solo el rol "General" puede desactivar cuentas; no puede desactivar la propia ni la última General activa.
    /// El motivo (opcional) queda en el historial junto con quién lo hizo.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> Desactivar(int id, [FromBody] CambiarEstadoRequest? request = null) =>
        this.Responder(await usuariosService.CambiarEstadoAsync(id, activo: false, IdUsuarioAutenticado, request?.Motivo));

    /// <summary>
    /// HU-AUT-006, escenario 3: reactiva una cuenta previamente desactivada.
    /// Solo el rol "General" puede reactivar cuentas. El motivo (opcional) queda en el historial.
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [Authorize(Roles = "General")]
    public async Task<IActionResult> Reactivar(int id, [FromBody] CambiarEstadoRequest? request = null) =>
        this.Responder(await usuariosService.CambiarEstadoAsync(id, activo: true, IdUsuarioAutenticado, request?.Motivo));
}
