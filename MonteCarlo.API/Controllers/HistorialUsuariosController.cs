using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>Historial de cambios de cuentas de administrador (HU-AUT-007). Solo el rol "General".</summary>
[ApiController]
[Route("api/historial-usuarios")]
[Authorize(Roles = "General")]
public class HistorialUsuariosController(IHistorialUsuarioService historialService) : ControllerBase
{
    /// <summary>
    /// Historial de cuentas, del más reciente al más antiguo, paginado (máximo 10 por página).
    /// </summary>
    /// <param name="busqueda">Texto contenido en el nombre, usuario o correo de la cuenta afectada.</param>
    /// <param name="idUsuarioAfectado">Solo los cambios de esa cuenta.</param>
    /// <param name="idUsuario">Solo los cambios hechos por ese administrador.</param>
    /// <param name="accion">Creada, Editada, Deshabilitada o Habilitada.</param>
    /// <param name="pagina">Página a mostrar (desde 1).</param>
    /// <param name="tamano">Filas por página; nunca más de 10.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? busqueda, [FromQuery] int? idUsuarioAfectado, [FromQuery] int? idUsuario,
        [FromQuery] string? accion, [FromQuery] int pagina = 1, [FromQuery] int tamano = IHistorialUsuarioService.TamanoPagina) =>
        this.Responder(await historialService.ObtenerAsync(busqueda, idUsuarioAfectado, idUsuario, accion, pagina, tamano));

    /// <summary>Administradores que hicieron algún cambio (para el filtro «Realizado por»).</summary>
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerResponsables() =>
        this.Responder(await historialService.ObtenerResponsablesAsync());
}
