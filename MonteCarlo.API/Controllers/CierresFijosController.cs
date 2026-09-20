using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;


/// HU-CFG-002: días fijos de cierre semanal.
/// Solo el rol "General" puede consultarlos y modificarlos.

[ApiController]
[Route("api/cierres-fijos")]
[Authorize(Roles = "General")]
public class CierresFijosController(ICierresFijosService cierresFijosService) : ControllerBase
{
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var result = await cierresFijosService.ObtenerAsync();
        return StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarCierresFijosRequest request)
    {
        var result = await cierresFijosService.ActualizarAsync(request.Dias, IdUsuarioAutenticado);

        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
    }
}
