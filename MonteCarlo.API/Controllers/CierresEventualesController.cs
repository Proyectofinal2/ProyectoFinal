using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// Cierres eventuales en fechas puntuales y manejo de las reservas existentes.
[ApiController]
[Route("api/cierres-eventuales")]
[Authorize(Roles = "General")]
public class CierresEventualesController(ICierresEventualesService cierresEventualesService) : ControllerBase
{
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var result = await cierresEventualesService.ObtenerAsync();
        return Responder(result);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarCierreEventualRequest request)
    {
        var result = await cierresEventualesService.RegistrarAsync(
            request.Fecha!.Value, request.Motivo!, request.CancelarReservas, IdUsuarioAutenticado);
        return Responder(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var result = await cierresEventualesService.EliminarAsync(id, IdUsuarioAutenticado);
        return Responder(result);
    }

    private IActionResult Responder<T>(Result<T> result) =>
        StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message));
}
