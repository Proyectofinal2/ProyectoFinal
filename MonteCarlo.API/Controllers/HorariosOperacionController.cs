using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>HU-CFG-005: horarios regulares de operación por día de la semana.</summary>
[ApiController]
[Route("api/horarios-operacion")]
[Authorize(Roles = "General")]
public class HorariosOperacionController(IHorariosOperacionService horariosOperacionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var result = await horariosOperacionService.ObtenerAsync();
        return StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    [HttpPut]
    public async Task<IActionResult> Guardar([FromBody] GuardarHorarioOperacionRequest request)
    {
        var result = await horariosOperacionService.GuardarAsync(request.DiaSemana,
            request.HoraApertura, request.HoraCierre);
        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
    }
}
