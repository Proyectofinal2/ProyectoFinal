using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// Reservas del dia para planificar la operacion y preparar la atencion.
[Area("Admin")]
[Route("admin/reservas-del-dia")]
[Authorize(Roles = "General,Regular")]
public class ReservasDelDiaController(IReservasApiService reservasApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? fecha)
    {
        var modelo = new ReservasDelDiaViewModel { Fecha = fecha ?? DateOnly.FromDateTime(DateTime.Now) };

        var (success, message, data) = await reservasApiService.ObtenerReservasDelDiaAsync(modelo.Fecha);

        if (success)
        {
            modelo.Reservas = data ?? [];
        }
        else
        {
            TempData[Alerta.Error] = message;
        }

        return View(modelo);
    }
}
