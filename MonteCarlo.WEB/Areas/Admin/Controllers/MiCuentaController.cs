using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-AUT-004 — Cambio de contraseña desde Mi Cuenta.
/// Solo usuarios autenticados pueden acceder.
/// </summary>
[Area("Admin")]
[Route("admin/mi-cuenta")]
[Authorize]
public class MiCuentaController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        // TODO conectar: leer los datos de la cuenta desde los claims.
        return View(new CambiarContrasenaViewModel());
    }

    [HttpPost("contrasena")]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarContrasena(CambiarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), modelo);
        }

        // TODO conectar: verificar contrasena actual y aplicar la nueva.
        // No cerrar sesion tras el cambio (refrescar cookie si se revocan otras sesiones).

        TempData[Alerta.Exito] = "Tu contrasena fue actualizada.";
        return RedirectToAction(nameof(Index));
    }
}
