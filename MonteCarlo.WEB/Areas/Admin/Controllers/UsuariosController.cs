using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-AUT-005 (crear cuentas), HU-AUT-006 (consultar, desactivar y reactivar) y
/// HU-AUT-007 (editar cuentas; cada cambio queda en el historial con el administrador responsable).
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
    public async Task<IActionResult> Index(string? busqueda, FiltroEstadoCuenta estado = FiltroEstadoCuenta.Todas) =>
        View(await ConstruirIndexAsync(busqueda, estado));

    private async Task<CuentasIndexViewModel> ConstruirIndexAsync(string? busqueda, FiltroEstadoCuenta estado)
    {
        // Se pide la busqueda completa y el estado se filtra aqui: asi el control segmentado puede mostrar
        // cuantas cuentas hay en cada estado sin otra llamada.
        var (success, message, data) = await usuariosApiService.ObtenerTodosAsync(busqueda, FiltroEstadoCuenta.Todas);

        List<CuentaFilaViewModel> cuentas = [];
        int totalActivas = 0, totalInactivas = 0;

        if (success && data != null)
        {
            totalActivas = data.Count(u => u.Activo);
            totalInactivas = data.Count - totalActivas;

            cuentas = data
                .Where(u => estado switch
                {
                    FiltroEstadoCuenta.Activas => u.Activo,
                    FiltroEstadoCuenta.Inactivas => !u.Activo,
                    _ => true
                })
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

        return new CuentasIndexViewModel
        {
            Busqueda = busqueda,
            Estado = estado,
            Cuentas = cuentas,
            TotalCuentas = totalActivas + totalInactivas,
            TotalActivas = totalActivas,
            TotalInactivas = totalInactivas
        };
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
    // HU-AUT-007 — Editar cuenta (modal)
    // ------------------------------------------------------------

    /// <summary>Devuelve solo el formulario para el fetch del modal.</summary>
    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id)
    {
        var (success, _, cuenta) = await usuariosApiService.ObtenerPorIdAsync(id);
        if (!success || cuenta is null)
            return NotFound();

        var formulario = new EditarCuentaViewModel
        {
            IdUsuario = cuenta.IdUsuario,
            NombreCompleto = cuenta.NombreCompleto,
            NombreUsuario = cuenta.NombreUsuario,
            CorreoElectronico = cuenta.CorreoElectronico,
            CorreoPersonal = cuenta.CorreoPersonal,
            Rol = cuenta.Rol,
            Original = cuenta
        };

        return await MarcarRestriccionesAsync(formulario, cuenta.Activo, cuenta.Rol)
            ? PartialView("_CuentaFormulario", formulario)
            : StatusCode(503);
    }

    [HttpPost("{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int id, EditarCuentaViewModel modelo)
    {
        modelo.IdUsuario = id;

        if (ModelState.IsValid)
        {
            var (success, message, _) = await usuariosApiService.ActualizarAsync(id, modelo);

            if (success)
            {
                TempData[Alerta.Exito] = message;
                return RedirectToAction(nameof(Index));
            }

            // Duplicados: error de campo, para que se vea cual de los dos se repitio.
            if (message.Contains("nombre de usuario"))
                ModelState.AddModelError(nameof(modelo.NombreUsuario), message);
            else if (message.Contains("correo"))
                ModelState.AddModelError(nameof(modelo.CorreoElectronico), message);
            else
                ModelState.AddModelError(string.Empty, message);
        }

        // Se vuelve a pintar el listado con el panel abierto y los errores.
        var (okCuenta, _, cuenta) = await usuariosApiService.ObtenerPorIdAsync(id);
        if (okCuenta && cuenta is not null)
        {
            modelo.Original = cuenta;
            await MarcarRestriccionesAsync(modelo, cuenta.Activo, cuenta.Rol);
        }
        else
            modelo.EsCuentaPropia = id == IdUsuarioEnSesion;

        var indice = await ConstruirIndexAsync(null, FiltroEstadoCuenta.Todas);
        indice.Formulario = modelo;
        return View(nameof(Index), indice);
    }

    /// <summary>
    /// Marca si el rol no se puede cambiar: cuenta propia o unica cuenta General activa.
    /// Es una ayuda visual; la API aplica las mismas reglas.
    /// </summary>
    private async Task<bool> MarcarRestriccionesAsync(EditarCuentaViewModel formulario, bool activa, string rolActual)
    {
        formulario.EsCuentaPropia = formulario.IdUsuario == IdUsuarioEnSesion;

        if (rolActual == Roles.General && activa)
        {
            var (success, _, activas) = await usuariosApiService.ObtenerTodosAsync(null, FiltroEstadoCuenta.Activas);
            if (!success || activas is null)
                return false;

            formulario.EsUltimoGeneralActivo = activas.Count(c => c.Rol == Roles.General) <= 1;
        }

        return true;
    }

    // ------------------------------------------------------------
    // HU-AUT-006, escenarios 2, 3 y 4 — Cambio de estado
    // ------------------------------------------------------------

    [HttpPost("{id:int}/desactivar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id, string? motivo)
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

        var (success, message, _) = await usuariosApiService.DesactivarAsync(id, motivo);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/reactivar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? motivo)
    {
        var (success, message, _) = await usuariosApiService.ReactivarAsync(id, motivo);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }
}
