using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;


/// HU-CFG-001 — Umbral de confirmación automática de reservas.
/// Solo el rol "General"; la API vuelve a validar el rol.

[Area("Admin")]
[Route("admin/parametros")]
[Authorize(Roles = Roles.General)]
public class ParametrosController(IConfiguracionApiService configuracionApiService) : Controller
{
    /// Escenario 1: muestra el valor vigente (4 por defecto).
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (success, message, data) = await configuracionApiService.ObtenerUmbralAsync();

        if (!success)
        {
            TempData[Alerta.Error] = message;
        }

        return View(new UmbralViewModel
        {
            Umbral = data?.Umbral,
            FechaModificacion = data?.FechaModificacion,
            UsuarioModificacion = data?.UsuarioModificacion
        });
    }

    /// Escenario 2: guarda un nuevo umbral entero positivo.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UmbralViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var (success, message, _) = await configuracionApiService.ActualizarUmbralAsync(modelo.Umbral!.Value);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }
}
