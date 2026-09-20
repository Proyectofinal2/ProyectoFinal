using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;


/// HU-CFG-002 — Días fijos de cierre semanal.
/// Solo el rol "General"; la API vuelve a validar el rol.

[Area("Admin")]
[Route("admin/cierres")]
[Authorize(Roles = Roles.General)]
public class CierresController(IConfiguracionApiService configuracionApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (success, message, data) = await configuracionApiService.ObtenerCierresFijosAsync();

        if (!success)
        {
            TempData[Alerta.Error] = message;
        }

        return View(new CierresFijosViewModel { Dias = data?.Dias ?? [] });
    }

    /// Escenarios 1 y 2: la selección enviada es la lista completa de días cerrados;
    /// los que se marcaron se agregan y los que se desmarcaron se quitan.
   
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CierresFijosViewModel modelo)
    {
        var (success, message, _) = await configuracionApiService.ActualizarCierresFijosAsync(modelo.Dias);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }
}
