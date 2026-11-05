using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Ventas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// Cuenta de la mesa y cobro (HU-CON-002). Los GET devuelven parciales HTML (_Cuenta y _CajaPedido) que cargan por
/// fetch las pantallas anfitrionas (Mesas, Reservas del dia, Ventas); los POST son JSON con antiforgery por cabecera
/// y actuan como proxy hacia la API. Si la cuenta cambio (409) la respuesta trae la cuenta actual en <c>cuenta</c>.
/// </summary>
[Area("Admin")]
[Route("admin/ventas")]
[Authorize(Roles = $"{Roles.General},{Roles.Regular}")]
public class VentasController(IVentasApiService ventasApiService) : Controller
{
    /// <summary>Cuerpo de la cuenta: pedidos, total y controles de cobro segun lo que la API permita.</summary>
    [HttpGet("cuenta/{idReserva:int}")]
    public async Task<IActionResult> Cuenta(int idReserva)
    {
        var (success, message, cuenta) = await ventasApiService.ObtenerCuentaAsync(idReserva);
        if (!success || cuenta is null)
            return ErrorParcial(message);

        var (_, _, metodos) = await ventasApiService.ObtenerMetodosPagoAsync();
        return PartialView("_Cuenta", new CuentaViewModel { Cuenta = cuenta, MetodosPago = metodos ?? [] });
    }

    /// <summary>Caja de pedido (modal): el menu vendible y la cuenta a la que se agrega.</summary>
    [HttpGet("cuenta/{idReserva:int}/caja")]
    public async Task<IActionResult> Caja(int idReserva)
    {
        var (success, message, cuenta) = await ventasApiService.ObtenerCuentaAsync(idReserva);
        if (!success || cuenta is null)
            return ErrorParcial(message);
        if (!cuenta.PuedeAgregar)
            return ErrorParcial(cuenta.Bloqueo ?? "Esta cuenta no admite mas pedidos.");

        var (okMenu, mensajeMenu, categorias) = await ventasApiService.ObtenerCatalogoVentaAsync();
        if (!okMenu)
            return ErrorParcial(mensajeMenu);

        return PartialView("_CajaPedido", new CajaViewModel { Cuenta = cuenta, Categorias = categorias ?? [] });
    }

    [HttpPost("cuenta/{idReserva:int}/pedidos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarPedido(int idReserva, [FromBody] PedidoBody body)
    {
        var lineas = (body.Lineas ?? []).Select(l => (l.IdProducto, l.Cantidad)).ToList();
        var (success, message, data, cuenta) = await ventasApiService.AgregarPedidoAsync(idReserva, body.ClaveIdempotencia, lineas);
        return Json(new { success, message, data, cuenta });
    }

    [HttpPost("cuenta/lineas/{idDetalle:int}/anular")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnularLinea(int idDetalle, [FromBody] AnularLineaBody body)
    {
        var (success, message, data, cuenta) = await ventasApiService.AnularLineaAsync(idDetalle, body.Cantidad, body.Motivo);
        return Json(new { success, message, data, cuenta });
    }

    [HttpPost("cuenta/{idReserva:int}/cobrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cobrar(int idReserva, [FromBody] CobrarBody body)
    {
        var (success, message, data, cuenta) = await ventasApiService.CobrarAsync(idReserva, body.IdMetodoPago, body.TotalEsperado, body.Version);
        return Json(new { success, message, data, cuenta });
    }

    [HttpPost("cuenta/{idReserva:int}/liberar-sin-consumo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LiberarSinConsumo(int idReserva)
    {
        var (success, message, data, cuenta) = await ventasApiService.LiberarSinConsumoAsync(idReserva);
        return Json(new { success, message, data, cuenta });
    }

    private ContentResult ErrorParcial(string mensaje)
    {
        Response.StatusCode = StatusCodes.Status409Conflict;
        return Content(
            $"<div class=\"alert alert-danger mb-0\" role=\"alert\">{System.Net.WebUtility.HtmlEncode(mensaje)}</div>",
            "text/html");
    }

    public record LineaBody(int IdProducto, int Cantidad);
    public record PedidoBody(Guid ClaveIdempotencia, List<LineaBody>? Lineas);
    public record AnularLineaBody(int? Cantidad, string? Motivo);
    public record CobrarBody(int IdMetodoPago, decimal TotalEsperado, string? Version);

    /// <summary>HU-CON-004: Listado de ventas con filtros.</summary>
    [HttpGet("listado")]
    public async Task<IActionResult> Index(FiltroVentasViewModel filtro)
    {
        // Si no hay fechas, por defecto busca hoy
        if (!filtro.FechaInicio.HasValue)
        {
            filtro.FechaInicio = DateTime.Today;
            filtro.FechaFin = DateTime.Today;
        }

        var (success, message, ventas) = await ventasApiService.ObtenerListadoAsync(filtro);

        if (success)
            filtro.Ventas = ventas ?? [];
        else
            TempData["Alerta.Error"] = message;

        // Cargamos los métodos de pago para el select
        var (_, _, metodos) = await ventasApiService.ObtenerMetodosPagoAsync();
        filtro.MetodosPago = metodos ?? [];

        // Le decimos explícitamente dónde está la vista
        return View("~/Areas/Admin/Views/Ventas/Index.cshtml", filtro);
    }

    [HttpGet("listado/detalle/{idVenta:int}")]
    public async Task<IActionResult> Detalle(int idVenta)
    {
        var (success, message, data) = await ventasApiService.ObtenerDetalleAsync(idVenta);
        if (!success || data is null)
            return Content($"<div class='alert alert-danger'>{message}</div>", "text/html");

        return PartialView("DetalleVenta", data);
    }
}
