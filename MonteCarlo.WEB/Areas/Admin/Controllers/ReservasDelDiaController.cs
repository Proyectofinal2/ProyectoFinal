using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// Reservas (por defecto, las de hoy; admite un rango de fechas) para planificar la operacion y preparar la atencion.
[Area("Admin")]
[Route("admin/reservas-del-dia")]
[Authorize(Roles = "General,Regular")]
public class ReservasDelDiaController(IReservasApiService reservasApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? desde, DateOnly? hasta, string? buscar, string? tipo, int pagina = 1)
    {
        var inicio = desde ?? hasta ?? DateOnly.FromDateTime(DateTime.Now);
        var fin = hasta ?? inicio;
        if (fin < inicio) (inicio, fin) = (fin, inicio);

        var modelo = new ReservasDelDiaViewModel
        {
            Desde = inicio,
            Hasta = fin,
            TerminoBusqueda = buscar?.Trim(),
            Tipo = tipo is "walkin" or "reserva" ? tipo : ""
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
                await reservasApiService.ObtenerReservasDelDiaAsync(modelo.Desde, modelo.Hasta);

            if (success)
            {
                modelo.Reservas = data ?? [];
            }
            else
            {
                TempData[Alerta.Error] = message;
            }
        }

        modelo.Pagina = Math.Clamp(pagina, 1, modelo.TotalPaginas);

        return View(modelo);


    }
    [HttpPost("atender")] // <-- Ruta única
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Atender([FromForm] string codigo, [FromForm] DateOnly desde, [FromForm] DateOnly hasta)
    {
        var (success, message) = await reservasApiService.RegistrarAtencionAsync(codigo);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return VolverAlListado(desde, hasta);
    }

    [HttpPost("noshow")] // <-- Ruta única
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow([FromForm] string codigo, [FromForm] DateOnly desde, [FromForm] DateOnly hasta)
    {
        var (success, message) = await reservasApiService.RegistrarNoShowAsync(codigo);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return VolverAlListado(desde, hasta);
    }

    [HttpPost("cambiar-estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado([FromForm] string codigo, [FromForm] string nuevoEstado, [FromForm] DateOnly desde, [FromForm] DateOnly hasta)
    {
        var (success, message) = await reservasApiService.CambiarEstadoAsync(codigo, nuevoEstado);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return VolverAlListado(desde, hasta);
    }

    private RedirectToActionResult VolverAlListado(DateOnly desde, DateOnly hasta) =>
        RedirectToAction(nameof(Index), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd") });

}