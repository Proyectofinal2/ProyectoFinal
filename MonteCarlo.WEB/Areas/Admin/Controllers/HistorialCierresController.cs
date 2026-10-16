using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// Historial de cambios en cierres fijos y eventuales.
[Area("Admin")]
[Route("admin/historial-cierres")]
[Authorize(Roles = Roles.General)]
public class HistorialCierresController(IConfiguracionApiService configuracionApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? tipo, string? accion, DateOnly? desde, DateOnly? hasta)
    {
        var modelo = new HistorialCierresViewModel { Tipo = tipo, Accion = accion, Desde = desde, Hasta = hasta };

        var (success, message, data) = await configuracionApiService.ObtenerHistorialCierresAsync(
            tipo, accion, desde, hasta);

        if (success)
        {
            modelo.Items = data ?? [];
        }
        else
        {
            TempData[Alerta.Error] = message;
        }

        return View(modelo);
    }
}
