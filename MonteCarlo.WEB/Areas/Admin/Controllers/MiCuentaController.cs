using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-AUT-004 — Cambio de contraseña desde Mi Cuenta.
/// Solo usuarios autenticados pueden acceder.
/// </summary>
[Area("Admin")]
[Route("admin/mi-cuenta")]
[Authorize]
public class MiCuentaController(IAuthApiService authApiService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        // TODO conectar: leer los datos de la cuenta desde los claims.
        return View(new CambiarContrasenaViewModel());
    }

    [HttpPost("contrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), modelo);
        }

        var (success, message) = await authApiService.CambiarContrasenaAsync(
            modelo.ContrasenaActual,
            modelo.NuevaContrasena);

        if (!success)
        {
            if (message.Contains("contraseña actual", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(modelo.ContrasenaActual), message);
            }
            else if (message.Contains("nueva contraseña", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(modelo.NuevaContrasena), message);
            }
            else
            {
                ModelState.AddModelError(string.Empty, message);
            }

            return View(nameof(Index), modelo);
        }

        TempData[Alerta.Exito] = message;
        return RedirectToAction(nameof(Index));
    }
}
