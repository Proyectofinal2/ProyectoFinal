using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;


/// HU-CFG-001 / HU-CFG-006 - Parametros del sistema. Pantalla dinamica: pinta un
/// input por cada fila que la API devuelva desde ConfiguracionSistema, y solo
/// envia lo que realmente cambio (ParametrosViewModel.ObtenerCambios).

[Area("Admin")]
[Route("admin/parametros")]
[Authorize(Roles = Roles.General)]
public class ParametrosController(IConfiguracionApiService configuracionApiService) : Controller
{
    /// Escenario 1: muestra los parametros que existan en ese momento en la base de datos.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (success, message, data) = await configuracionApiService.ObtenerConfiguracionGeneralAsync();

        if (!success)
        {
            TempData[Alerta.Error] = message;
        }

        return View(new ParametrosViewModel
        {
            Parametros = (data?.Parametros ?? []).Select(p => new ParametroViewModel
            {
                Clave = p.Clave,
                Etiqueta = p.Etiqueta,
                Descripcion = p.Descripcion,
                Valor = p.Valor,
                ValorOriginal = p.Valor,
                FechaModificacion = p.FechaModificacion,
                UsuarioModificacion = p.UsuarioModificacion
            }).ToList()
        });
    }

    /// Escenario 2: detecta que campos cambiaron y solo esos se envian a la API.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ParametrosViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var cambios = modelo.ObtenerCambios();

        if (cambios.Count == 0)
        {
            TempData[Alerta.Exito] = "No había cambios que guardar.";
            return RedirectToAction(nameof(Index));
        }

        var (success, message, _) = await configuracionApiService.ActualizarConfiguracionGeneralAsync(cambios);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }
}
