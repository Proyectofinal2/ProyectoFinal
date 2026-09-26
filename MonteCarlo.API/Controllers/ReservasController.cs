using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ReservasController(IReservasService reservasService) : ControllerBase
{
    [HttpGet("disponibilidad")]
    public async Task<IActionResult> ObtenerDisponibilidad([FromQuery] int cantidadPersonas, [FromQuery] DateOnly fecha)
    {
        var result = await reservasService.ObtenerDisponibilidadAsync(cantidadPersonas, fecha);
        return Responder(result);
    }

    [HttpGet("disponibilidad/mes")]
    public async Task<IActionResult> ObtenerDisponibilidadMes([FromQuery] int cantidadPersonas, [FromQuery] int anio, [FromQuery] int mes)
    {
        var result = await reservasService.ObtenerDisponibilidadMesAsync(cantidadPersonas, anio, mes);
        return Responder(result);
    }

    [HttpGet("{codigo}")]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo)
    {
        var result = await reservasService.ObtenerPorCodigoAsync(codigo);
        return Responder(result);
    }

    [HttpPost("{codigo}/cancelar")]
    public async Task<IActionResult> Cancelar(string codigo)
    {
        var result = await reservasService.CancelarAsync(codigo);
        return Responder(result);
    }

    private IActionResult Responder<T>(Result<T> result) =>
        StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
}