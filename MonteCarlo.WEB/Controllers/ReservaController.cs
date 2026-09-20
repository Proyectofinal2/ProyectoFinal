using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Controllers;


public class ReservaController(IReservasApiService reservasApiService) : Controller
{
    // HU-RES-002 Muestra el calendario

    [HttpGet]
    public IActionResult Create()
    {
        return View(new NuevaReservaDisponibilidadViewModel());
    }

    // ------------------------------------------------------------
    // HU-RES-005 — Consultar y cancelar una reserva con su código
    // ------------------------------------------------------------


    /// Sin código muestra el formulario; con código busca la reserva y,
    /// si se puede cancelar, ofrece el botón de cancelación.
    
    [HttpGet]
    public async Task<IActionResult> Consultar(string? codigo)
    {
        var modelo = new ConsultarReservaViewModel { Codigo = codigo?.Trim() };

        if (string.IsNullOrWhiteSpace(modelo.Codigo))
        {
            return View(modelo);
        }

        var (success, message, reserva) = await reservasApiService.ObtenerPorCodigoAsync(modelo.Codigo);

        if (success)
        {
            modelo.Reserva = reserva;
        }
        else
        {
            ModelState.AddModelError(nameof(modelo.Codigo), message);
        }

        return View(modelo);
    }

    /// Escenario 1: cancela la reserva Pendiente/Confirmada y confirma al cliente.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(string codigo)
    {
        var (success, message, _) = await reservasApiService.CancelarAsync(codigo);

        TempData[success ? Alerta.Exito : Alerta.Error] = message;

        return RedirectToAction(nameof(Consultar), new { codigo });
    }
}
