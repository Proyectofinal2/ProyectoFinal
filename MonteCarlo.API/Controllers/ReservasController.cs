using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservasController(IReservasService reservasService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("disponibilidad")]
    public async Task<IActionResult> ObtenerDisponibilidad([FromQuery] int cantidadPersonas, [FromQuery] DateOnly fecha)
    {
        var result = await reservasService.ObtenerDisponibilidadAsync(cantidadPersonas, fecha);
        return Responder(result);
    }

    [AllowAnonymous]
    [HttpGet("disponibilidad/mes")]
    public async Task<IActionResult> ObtenerDisponibilidadMes([FromQuery] int cantidadPersonas, [FromQuery] int anio, [FromQuery] int mes)
    {
        var result = await reservasService.ObtenerDisponibilidadMesAsync(cantidadPersonas, anio, mes);
        return Responder(result);
    }

    /// <summary>HU-MES-005/006: sugiere la mejor asignación de mesa para una reserva existente, sin aplicarla.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpGet("{id:int}/sugerencia-mesa")]
    public async Task<IActionResult> SugerirAsignacionPorReserva(int id)
    {
        var result = await reservasService.SugerirAsignacionPorReservaAsync(id);
        return Responder(result);
    }

    /// <summary>
    /// HU-MES-005/006: asigna o reasigna manualmente 1 o 2 mesas a una reserva
    /// (aplica tanto a una reserva Pendiente sin mesa como a una Confirmada que se reasigna).
    /// </summary>
    [Authorize(Roles = "General,Regular")]
    [HttpPost("{id:int}/asignar-mesa")]
    public async Task<IActionResult> AsignarMesa(int id, [FromBody] AsignarMesaRequest request)
    {
        var result = await reservasService.AsignarMesaAsync(id, request.IdsMesa);
        return Responder(result);
    }

    [AllowAnonymous]
    [HttpGet("{codigo}")]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo)
    {
        var result = await reservasService.ObtenerPorCodigoAsync(codigo);
        return Responder(result);
    }

    [AllowAnonymous]
    [HttpPost("{codigo}/cancelar")]
    public async Task<IActionResult> Cancelar(string codigo)
    {
        var result = await reservasService.CancelarAsync(codigo);
        return Responder(result);
    }

    /// <summary>
    /// HU-RES-003: registra una reserva nueva desde el formulario público.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearReservaRequest request)
    {
        var result = await reservasService.CrearAsync(request);
        return Responder(result);
    }

    private IActionResult Responder<T>(Result<T> result) =>
        StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
}