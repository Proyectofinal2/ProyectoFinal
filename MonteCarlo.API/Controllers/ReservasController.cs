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

    /// <summary>HU-MES-005/006: asigna o reasigna manualmente 1 o 2 mesas a una reserva.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpPost("{id:int}/asignar-mesa")]
    public async Task<IActionResult> AsignarMesa(int id, [FromBody] AsignarMesaRequest request)
    {
        var result = await reservasService.AsignarMesaAsync(id, request.IdsMesa);
        return Responder(result);
    }

    /// Reservas del dia (hoy si no se indica fecha).
    [Authorize(Roles = "General,Regular")]
    [HttpGet("del-dia")]
    public async Task<IActionResult> ObtenerDelDia([FromQuery] DateOnly? fecha)
    {
        var result = await reservasService.ObtenerDelDiaAsync(fecha ?? DateOnly.FromDateTime(DateTime.Now));
        return Responder(result);
    }

    /// <summary>HU-REP-001: catalogo real de estados para el filtro de reportes.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpGet("estados")]
    public async Task<IActionResult> ObtenerEstadosReporte()
    {
        var result = await reservasService.ObtenerEstadosReporteAsync();
        return Responder(result);
    }

    /// <summary>HU-REP-001: indicadores y distribucion de reservas por periodo.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpGet("reporte")]
    public async Task<IActionResult> ObtenerReporte(
        [FromQuery] DateOnly? fechaInicio,
        [FromQuery] DateOnly? fechaFin,
        [FromQuery] string? estado)
    {
        var result = await reservasService.ObtenerReporteAsync(fechaInicio, fechaFin, estado);
        return Responder(result);
    }

    /// HU-OPE-005: busca reservas por nombre o teléfono del cliente
    [Authorize(Roles = "General,Regular")]
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termino)
    {
        var result = await reservasService.BuscarAsync(termino);
        return Responder(result);
    }

    /// <summary>HU-OPE-003: registra la llegada del cliente.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpPost("{codigo}/atender")]
    public async Task<IActionResult> Atender(string codigo)
    {
        var result = await reservasService.RegistrarAtencionAsync(codigo);
        return Responder(result);
    }

    /// <summary>HU-OPE-003: marca la reserva como No Show.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpPost("{codigo}/no-show")]
    public async Task<IActionResult> NoShow(string codigo)
    {
        var result = await reservasService.RegistrarNoShowAsync(codigo);
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

    /// <summary>HU-OPE-004: sienta un cliente sin reserva previa en 1 o 2 mesas elegidas por el admin.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpPost("walkin")]
    public async Task<IActionResult> CrearWalkIn([FromBody] CrearReservaWalkInRequest request)
    {
        var result = await reservasService.CrearWalkInAsync(request);
        return Responder(result);
    }

    /// <summary>HU-OPE-002: Cambia el estado de una reserva manualmente.</summary>
    [Authorize(Roles = "General,Regular")]
    [HttpPost("{codigo}/cambiar-estado")]
    public async Task<IActionResult> CambiarEstado(string codigo, [FromQuery] string nuevoEstado)
    {
        var result = await reservasService.CambiarEstadoAsync(codigo, nuevoEstado);
        return Responder(result);
    }

    // Método para respuestas con Data (Result<T>)
    private IActionResult Responder<T>(Result<T> result) =>
        StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));

    // Método para respuestas sin Data (Result)
    private IActionResult Responder(Result result) =>
        StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message)
            : ApiResponse.ErrorResponse(result.Message));
}
