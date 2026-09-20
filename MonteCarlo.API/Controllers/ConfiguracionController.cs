using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;


/// HU-CFG-001: umbral de confirmación automática de reservas.
/// Solo el rol "General" puede consultarlo y modificarlo.

[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = "General")]
public class ConfiguracionController(IConfiguracionService configuracionService) : ControllerBase
{
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("umbral-confirmacion")]
    public async Task<IActionResult> ObtenerUmbral()
    {
        var result = await configuracionService.ObtenerUmbralAsync();
        return StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message, result.Data));
    }

    [HttpPut("umbral-confirmacion")]
    public async Task<IActionResult> ActualizarUmbral([FromBody] ActualizarUmbralRequest request)
    {
        var result = await configuracionService.ActualizarUmbralAsync(request.Umbral!.Value, IdUsuarioAutenticado);

        return StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
    }
}
