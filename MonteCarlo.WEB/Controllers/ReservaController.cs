using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Controllers;

public class ReservaController(IReservasApiService reservasApiService) : Controller
{
    // ============================================================
    // HU-RES-002 — Paso 1: selección de fecha, hora y personas
    // ============================================================

    [HttpGet]
    public IActionResult Create()
    {
        return View(new NuevaReservaDisponibilidadViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Disponibilidad(int cantidadPersonas, DateOnly fecha)
    {
        var (success, message, data) = await reservasApiService.ObtenerDisponibilidadAsync(cantidadPersonas, fecha);
        return Json(new { success, message, data });
    }

    [HttpGet]
    public async Task<IActionResult> DisponibilidadMes(int cantidadPersonas, int anio, int mes)
    {
        var (success, message, data) = await reservasApiService.ObtenerDisponibilidadMesAsync(cantidadPersonas, anio, mes);
        return Json(new { success, message, data });
    }

    // ============================================================
    // HU-RES-003 — Paso 2: formulario de datos del cliente
    // ============================================================

    [HttpGet]
    public IActionResult Datos(DateOnly fechaReserva, TimeOnly horaReserva, int cantidadPersonas)
    {
        // Validar que vengan los datos del paso 1.
        if (fechaReserva == default || horaReserva == default || cantidadPersonas <= 0)
        {
            TempData[Alerta.Error] = "Debes seleccionar fecha, hora y cantidad de personas primero.";
            return RedirectToAction(nameof(Create));
        }

        var modelo = new DatosReservaViewModel
        {
            FechaReserva = fechaReserva,
            HoraReserva = horaReserva,
            CantidadPersonas = cantidadPersonas
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Datos(DatosReservaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var (success, message, data) = await reservasApiService.CrearAsync(modelo);

        if (!success || data is null)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(modelo);
        }

        // Guardar el código en TempData para que la pantalla de confirmación lo lea.
        TempData["CodigoReserva"] = data.CodigoReserva;

        return RedirectToAction(nameof(Confirmacion), new { codigo = data.CodigoReserva });
    }

    // ============================================================
    // HU-RES-003 — Paso 3: pantalla de confirmación con código
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> Confirmacion(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return RedirectToAction(nameof(Create));
        }

        var (success, message, data) = await reservasApiService.ObtenerPorCodigoAsync(codigo);

        if (!success || data is null)
        {
            TempData[Alerta.Error] = message;
            return RedirectToAction(nameof(Create));
        }

        return View(data);
    }

    // ============================================================
    // HU-RES-005 — Consultar y cancelar reserva
    // ============================================================

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(string codigo)
    {
        var (success, message, reserva) = await reservasApiService.CancelarAsync(codigo);

        if (!success || reserva is null)
        {
            TempData[Alerta.Error] = message;
            return RedirectToAction(nameof(Consultar), new { codigo });
        }

        return View("Cancelada", reserva);
    }
}
