using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-AUT-005 (crear cuentas) y HU-AUT-006 (consultar, desactivar
/// y reactivar).
/// Solo el rol "General" puede acceder. Ocultar el enlace en el menú
/// no es suficiente: este atributo valida en el servidor.
/// </summary>
[Area("Admin")]
[Route("admin/usuarios")]
[Authorize(Roles = Roles.General)]
public class UsuariosController(IUsuariosApiService usuariosApiService) : Controller
{
    /// <summary>Id de la cuenta con la sesion activa (claim guardado en el login).</summary>
    private int IdUsuarioEnSesion =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ------------------------------------------------------------
    // HU-AUT-006, escenario 1 — Listado
    // ------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, FiltroEstadoCuenta estado = FiltroEstadoCuenta.Todas)
    {
        var (success, message, data) = await usuariosApiService.ObtenerTodosAsync(busqueda, estado);

        List<CuentaFilaViewModel> cuentas = [];

        if (success && data != null)
        {
            cuentas = data
                .Select(u => new CuentaFilaViewModel
                {
                    IdUsuario = u.IdUsuario,
                    NombreCompleto = u.NombreCompleto,
                    NombreUsuario = u.NombreUsuario,
                    CorreoElectronico = u.CorreoElectronico,
                    Rol = u.Rol,
                    Activa = u.Activo,
                    EsCuentaPropia = u.IdUsuario == IdUsuarioEnSesion
                })
                .ToList();
        }
        else if (!success)
        {
            TempData[Alerta.Error] = message;
        }

        return View(new CuentasIndexViewModel
        {
            Busqueda = busqueda,
            Estado = estado,
            Cuentas = cuentas
        });
    }

    // ------------------------------------------------------------
    // HU-AUT-005 — Alta de cuenta
    // ------------------------------------------------------------

    [HttpGet("nuevo")]
    public IActionResult Crear() => View(new NuevaCuentaViewModel());

    [HttpPost("nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NuevaCuentaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // Crear la cuenta en la API
        var (success, message, userData) = await usuariosApiService.CrearUsuarioAsync(modelo);

        if (!success)
        {
            // Escenario 2: correo o nombre de usuario duplicados son errores DE CAMPO
            // El usuario tiene que ver cual de los dos repitio
            if (message.Contains("nombre de usuario"))
            {
                ModelState.AddModelError(
                    nameof(modelo.NombreUsuario),
                    message);
            }
            else if (message.Contains("correo"))
            {
                ModelState.AddModelError(
                    nameof(modelo.CorreoElectronico),
                    message);
            }
            else
            {
                TempData[Alerta.Error] = message;
            }

            return View(modelo);
        }

        TempData[Alerta.Exito] =
            $"La cuenta de {modelo.NombreCompleto} fue creada y esta activa.";

        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------
    // HU-AUT-006, escenarios 2, 3 y 4 — Cambio de estado
    // ------------------------------------------------------------

    [HttpPost("{id:int}/desactivar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        // Escenario 4. La vista ya deshabilita el boton en la fila
        // propia, pero eso es solo una cortesia visual: la regla se
        // aplica aqui, donde no se puede evadir.
        if (id == IdUsuarioEnSesion)
        {
            TempData[Alerta.Error] =
                "No puedes desactivar la cuenta con la que iniciaste sesion.";
            return RedirectToAction(nameof(Index));
        }

        var (success, message, _) = await usuariosApiService.DesactivarAsync(id);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/reactivar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var (success, message, _) = await usuariosApiService.ReactivarAsync(id);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }
}
