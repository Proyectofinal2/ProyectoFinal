using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Ventas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/ventas/listado")] // Ruta exclusiva
[Authorize(Roles = "General,Regular")]
public class VentasListadoController(IVentasApiService ventasApiService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(FiltroVentasViewModel filtro)
    {
        // Si no hay fechas, por defecto busca hoy
        if (!filtro.FechaInicio.HasValue)
        {
            filtro.FechaInicio = DateTime.Today;
            filtro.FechaFin = DateTime.Today;
        }

        var (success, message, ventas) = await ventasApiService.ObtenerListadoAsync(filtro);

        if (success) filtro.Ventas = ventas ?? [];
        else TempData["Alerta.Error"] = message;

        var (_, _, metodos) = await ventasApiService.ObtenerMetodosPagoAsync();
        filtro.MetodosPago = metodos ?? [];

        return View("~/Areas/Admin/Views/Ventas/Index.cshtml", filtro);
    }

    [HttpGet("detalle/{idVenta:int}")]
    public async Task<IActionResult> Detalle(int idVenta)
    {
        var (success, message, data) = await ventasApiService.ObtenerDetalleAsync(idVenta);
        if (!success || data is null)
            return Content($"<div class='alert alert-danger'>{message}</div>", "text/html");

        return PartialView("~/Areas/Admin/Views/Ventas/DetalleVenta.cshtml", data);
    }
}