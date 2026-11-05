using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/metodos-pago")]
[Authorize(Roles = "General,Regular")]
public class MetodosPagoController(IConfiguracionApiService configuracionApiService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var (success, message, metodos) = await configuracionApiService.ObtenerMetodosPagoAdminAsync();
        if (!success) TempData["Alerta.Error"] = message;

        return View(new MetodosPagoViewModel { Metodos = metodos ?? [] });
    }

    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre)
    {
        var (success, message) = await configuracionApiService.CrearMetodoPagoAsync(nombre);
        TempData[success ? "Alerta.Exito" : "Alerta.Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cambiar-estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var (success, message) = await configuracionApiService.CambiarEstadoMetodoPagoAsync(id, activo);
        TempData[success ? "Alerta.Exito" : "Alerta.Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}