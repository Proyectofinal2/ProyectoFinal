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
        // Sin fechas: hoy. Con solo "desde": ese dia. Si el rango viene al reves, se invierte.
        filtro.FechaInicio ??= filtro.FechaFin ?? DateTime.Today;
        filtro.FechaFin ??= filtro.FechaInicio;
        if (filtro.FechaFin < filtro.FechaInicio)
            (filtro.FechaInicio, filtro.FechaFin) = (filtro.FechaFin, filtro.FechaInicio);

        // El origen (mesa / para llevar) se filtra aqui y no en el API: asi los conteos de cada origen salen del mismo
        // resultado y no cambian al elegir uno.
        var tipo = filtro.TipoVenta;
        filtro.TipoVenta = null;
        var (success, message, ventas) = await ventasApiService.ObtenerListadoAsync(filtro);
        filtro.TipoVenta = tipo;

        if (success)
        {
            // Una venta en ₡0 (por ejemplo, una cuenta abierta a la que se le quitaron todos los productos) no aporta nada al listado.
            var todas = (ventas ?? []).Where(v => v.Total > 0).ToList();
            filtro.ConteoConsumo = todas.Count(v => v.TipoVenta == "Consumo");
            filtro.ConteoParaLlevar = todas.Count(v => v.TipoVenta == "Takeout");
            filtro.Ventas = string.IsNullOrEmpty(tipo) ? todas : todas.Where(v => v.TipoVenta == tipo).ToList();
        }
        else TempData["Alerta.Error"] = message;

        filtro.Pagina = Math.Clamp(filtro.Pagina, 1, filtro.TotalPaginas);

        // El filtro ofrece todos los metodos, tambien los deshabilitados: hay ventas viejas que los usaron.
        var (_, _, metodos) = await ventasApiService.ObtenerMetodosPagoAsync(incluirInactivos: true);
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