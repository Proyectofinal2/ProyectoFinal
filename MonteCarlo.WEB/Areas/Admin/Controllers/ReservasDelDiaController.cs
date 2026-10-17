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
    public async Task<IActionResult> Index(DateOnly? fecha, string? buscar)
    {
        var modelo = new ReservasDelDiaViewModel
        {
            Fecha = fecha ?? DateOnly.FromDateTime(DateTime.Now),
            TerminoBusqueda = buscar?.Trim()
        };

        // HU-OPE-005: si hay busqueda activa, busca en todas las fechas;
        // de lo contrario, muestra las reservas del dia seleccionado.
        if (modelo.ModoBusqueda)
        {
            var (successBusqueda, messageBusqueda, dataBusqueda) =
                await reservasApiService.BuscarAsync(modelo.TerminoBusqueda!);

            if (successBusqueda)
            {
                modelo.Reservas = dataBusqueda ?? [];
            }
            else
            {
                TempData[Alerta.Error] = messageBusqueda;
            }
        }
        else
        {
            var (success, message, data) =
                await reservasApiService.ObtenerReservasDelDiaAsync(modelo.Fecha);

            if (success)
            {
                modelo.Reservas = data ?? [];
            }
            else
            {
                TempData[Alerta.Error] = message;
            }
        }

        return View(modelo);


    }
    [HttpPost("atender")] // <-- Ruta única
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Atender([FromForm] string codigo, [FromForm] DateOnly fecha)
    {
        var (success, message) = await reservasApiService.RegistrarAtencionAsync(codigo);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index), new { fecha = fecha.ToString("yyyy-MM-dd") });
    }

    [HttpPost("noshow")] // <-- Ruta única
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow([FromForm] string codigo, [FromForm] DateOnly fecha)
    {
        var (success, message) = await reservasApiService.RegistrarNoShowAsync(codigo);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index), new { fecha = fecha.ToString("yyyy-MM-dd") });
    }

    [HttpPost("cambiar-estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado([FromForm] string codigo, [FromForm] string nuevoEstado, [FromForm] DateOnly fecha)
    {
        var (success, message) = await reservasApiService.CambiarEstadoAsync(codigo, nuevoEstado);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index), new { fecha = fecha.ToString("yyyy-MM-dd") });
    }

}