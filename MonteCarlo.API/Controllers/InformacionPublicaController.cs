using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>Datos institucionales mínimos disponibles para visitantes.</summary>
[ApiController]
[Route("api/informacion-publica")]
public class InformacionPublicaController(IInformacionPublicaService informacionPublicaService) : ControllerBase
{
    /// <summary>Obtiene los horarios semanales visibles en la página principal.</summary>
    [AllowAnonymous]
    [HttpGet("horarios")]
    public async Task<IActionResult> ObtenerHorarios()
    {
        var result = await informacionPublicaService.ObtenerHorariosAsync();
        return this.Responder(result);
    }
}
