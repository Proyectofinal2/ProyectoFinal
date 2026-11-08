using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// HU-CFG-001 / HU-CFG-006: parametros dinamicos del sistema (ConfiguracionSistema).
/// No hay un set fijo de claves: se expone todo lo que exista en la tabla.
/// Solo el rol "General" puede consultarlos y modificarlos.
[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = "General")]
public class ConfiguracionController(IConfiguracionService configuracionService) : ControllerBase
{
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// Pantalla unica de Parametros: trae todos los parametros existentes en una sola llamada.
    [HttpGet]
    public async Task<IActionResult> ObtenerConfiguracionGeneral()
    {
        var result = await configuracionService.ObtenerConfiguracionGeneralAsync();
        return StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    /// Pantalla unica de Parametros: guarda, con un unico "Guardar cambios", solo los
    /// parametros recibidos (PATCH simplificado).
    [HttpPut]
    public async Task<IActionResult> ActualizarConfiguracionGeneral([FromBody] ActualizarConfiguracionGeneralRequest request)
    {
        var result = await configuracionService.ActualizarConfiguracionGeneralAsync(request, IdUsuarioAutenticado);

        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
    }

    // ============================================================
    // HU-CFG-007: Métodos de Pago
    // ============================================================

    [HttpGet("metodos-pago")]
    public async Task<IActionResult> ObtenerMetodosPagoAdmin()
    {
        var result = await configuracionService.ObtenerMetodosPagoAdminAsync();
        return StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    [HttpPost("metodos-pago")]
    public async Task<IActionResult> CrearMetodoPago([FromBody] CrearMetodoPagoRequest request)
    {
        var result = await configuracionService.CrearMetodoPagoAsync(request, IdUsuarioAutenticado);
        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
    }

    [HttpPut("metodos-pago/{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoMetodoPago(int id, [FromBody] CambiarEstadoMetodoPagoRequest request)
    {
        var result = await configuracionService.CambiarEstadoMetodoPagoAsync(id, request.Activo, IdUsuarioAutenticado);
        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message)
            : ApiResponse.ErrorResponse(result.Message));
    }
}
