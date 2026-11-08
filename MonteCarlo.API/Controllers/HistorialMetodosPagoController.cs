using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>Consulta de la auditoría de métodos de pago.</summary>
[ApiController]
[Route("api/historial-metodos-pago")]
[Authorize(Roles = "General")]
public class HistorialMetodosPagoController(
    IHistorialMetodoPagoService historialService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? busqueda, [FromQuery] int? idMetodoPago, [FromQuery] int? idUsuario,
        [FromQuery] string? accion, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = IHistorialMetodoPagoService.TamanoPagina) =>
        this.Responder(await historialService.ObtenerAsync(
            busqueda, idMetodoPago, idUsuario, accion, desde, hasta, pagina, tamano));

    [HttpGet("usuarios")]
    public async Task<IActionResult> ObtenerUsuarios() =>
        this.Responder(await historialService.ObtenerUsuariosAsync());
}
