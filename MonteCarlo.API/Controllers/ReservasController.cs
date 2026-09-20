using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// Acciones públicas del cliente sobre su reserva. No requieren cuenta:
/// el código único de la reserva es la credencial.
/// HU-RES-005: cancelar reserva con el código.

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ReservasController(IReservasService reservasService) : ControllerBase
{
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

	private IActionResult Responder(Result<ReservaResponse> result) =>
		StatusCode(result.StatusCode, result.Success
			? ApiResponse.SuccessResponse(result.Message, result.Data)
			: ApiResponse.ErrorResponse(result.Message));
}
