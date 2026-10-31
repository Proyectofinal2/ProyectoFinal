using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "General,Regular")]
public class MesasController(IMesasService mesasService, IReservasService reservasService) : ControllerBase
{
    /// <summary>HU-MES-002/HU-OPE-004: listado del canvas, completo solo si incluirDeshabilitadas.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirDeshabilitadas = false)
    {
        var result = await mesasService.ListarAsync(incluirDeshabilitadas);
        return Responder(result);
    }

    /// <summary>HU-DASH-001: indicadores actuales de disponibilidad y ocupacion.</summary>
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen()
    {
        var result = await mesasService.ObtenerResumenAsync();
        return Responder(result);
    }

    /// <summary>HU-OPE-004: sugerencia de mesa/combo para "ahora", sin reserva previa.</summary>
    [HttpGet("sugerencia-walkin")]
    public async Task<IActionResult> SugerenciaWalkIn([FromQuery] int cantidadPersonas)
    {
        var result = await reservasService.SugerirWalkInAsync(cantidadPersonas);
        return Responder(result);
    }

    /// <summary>HU-OPE-004 (gap #5): libera una mesa ocupada por abandono sin consumo.</summary>
    [HttpPut("{id:int}/liberar")]
    public async Task<IActionResult> Liberar(int id)
    {
        var result = await reservasService.LiberarMesaAsync(id);
        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message)
            : ApiResponse.ErrorResponse(result.Message));
    }

    // HU-MES-001/002/003/006: CRUD minimo de mesas, alcance del
    // companero de equipo (ver MesasService para el detalle).

    [Authorize(Roles = "General")]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearMesaRequest request)
    {
        var result = await mesasService.CrearAsync(request);
        return Responder(result);
    }

    [Authorize(Roles = "General")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarCapacidad(int id, [FromBody] ActualizarCapacidadRequest request)
    {
        var result = await mesasService.ActualizarCapacidadAsync(id, request);
        return Responder(result);
    }

    [Authorize(Roles = "General")]
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        var result = await mesasService.ActualizarEstadoAsync(id, request);
        return Responder(result);
    }

    [Authorize(Roles = "General")]
    [HttpPut("{id:int}/posicion")]
    public async Task<IActionResult> ActualizarPosicion(int id, [FromBody] ActualizarPosicionRequest request)
    {
        var result = await mesasService.ActualizarPosicionAsync(id, request);
        return Responder(result);
    }

    [Authorize(Roles = "General")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var result = await mesasService.EliminarAsync(id);
        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message)
            : ApiResponse.ErrorResponse(result.Message));
    }

    private IActionResult Responder<T>(Result<T> result) =>
        StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
}
