using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>Consulta de la auditoria de los horarios regulares de operacion.</summary>
[ApiController]
[Route("api/historial-horarios-operacion")]
[Authorize(Roles = "General")]
public class HistorialHorariosOperacionController(
    IHistorialHorarioOperacionService historialService) : ControllerBase
{
    /// <summary>Historial paginado, ordenado del cambio más reciente al más antiguo.</summary>
    [HttpGet]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? busqueda, [FromQuery] int? diaSemana, [FromQuery] int? idUsuario,
        [FromQuery] string? accion, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = IHistorialHorarioOperacionService.TamanoPagina) =>
        this.Responder(await historialService.ObtenerAsync(
            busqueda, diaSemana, idUsuario, accion, desde, hasta, pagina, tamano));

    /// <summary>Administradores que aparecen en el historial, para el filtro por usuario.</summary>
    [HttpGet("usuarios")]
    public async Task<IActionResult> ObtenerUsuarios() =>
        this.Responder(await historialService.ObtenerUsuariosAsync());
}
