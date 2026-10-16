using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// Historial de cambios en cierres fijos y eventuales.
[ApiController]
[Route("api/historial-cierres")]
[Authorize(Roles = "General")]
public class HistorialCierresController(IHistorialCierresService historialCierresService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? tipo, [FromQuery] string? accion, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        var result = await historialCierresService.ObtenerAsync(tipo, accion, desde, hasta);
        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
    }
}
