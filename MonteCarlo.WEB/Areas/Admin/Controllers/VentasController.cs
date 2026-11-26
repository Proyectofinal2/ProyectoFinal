using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Catalogo;
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

        // Se piden todos: el deshabilitado se muestra en gris y sin poder elegirse.
        var (_, _, metodos) = await ventasApiService.ObtenerMetodosPagoAsync(incluirInactivos: true);
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

        var tasa = cuenta.TasaIvaAplicada;
        if (cuenta.IdVenta is null)
        {
            var vigente = await ventasApiService.ObtenerTasaIvaAsync();
            if (!vigente.Success) return ErrorParcial(vigente.Message);
            tasa = vigente.Data;
        }
        return PartialView("_CajaPedido", new CajaViewModel { TasaEstimada = tasa, Cuenta = cuenta, Categorias = categorias ?? [] });
    }

    [HttpPost("cuenta/{idReserva:int}/pedidos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarPedido(int idReserva, [FromBody] PedidoBody body)
    {
        var lineas = (body.Lineas ?? []).Select(l => (l.IdProducto, l.Cantidad)).ToList();
        var (success, message, data, cuenta, conflicto) = await ventasApiService.AgregarPedidoAsync(idReserva, body.ClaveIdempotencia, lineas, body.TotalEsperado);
        return Json(new { success, message, data, cuenta, conflicto });
    }

    [HttpPost("cuenta/lineas/{idDetalle:int}/anular")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnularLinea(int idDetalle, [FromBody] AnularLineaBody body)
    {
        var (success, message, data, cuenta, conflicto) = await ventasApiService.AnularLineaAsync(idDetalle, body.Cantidad, body.Motivo);
        return Json(new { success, message, data, cuenta, conflicto });
    }

    [HttpPost("cuenta/{idReserva:int}/cobrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cobrar(int idReserva, [FromBody] CobrarBody body)
    {
        var (success, message, data, cuenta, conflicto) = await ventasApiService.CobrarAsync(idReserva, body.IdMetodoPago, body.TotalEsperado, body.Version);
        return Json(new { success, message, data, cuenta, conflicto });
    }

    [HttpPost("cuenta/{idReserva:int}/liberar-sin-consumo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LiberarSinConsumo(int idReserva)
    {
        var (success, message, data, cuenta, conflicto) = await ventasApiService.LiberarSinConsumoAsync(idReserva);
        return Json(new { success, message, data, cuenta, conflicto });
    }

    /// <summary>Venta para llevar (HU-CON-003): la caja como pagina, con metodo de pago y cobro.</summary>
    [HttpGet("para-llevar")]
    public async Task<IActionResult> ParaLlevar()
    {
        var (okMenu, mensajeMenu, categorias) = await ventasApiService.ObtenerCatalogoVentaAsync();
        var (_, _, metodos) = await ventasApiService.ObtenerMetodosPagoAsync(incluirInactivos: true);
        if (!okMenu)
            TempData[Alerta.Error] = mensajeMenu;

        var vigente = await ventasApiService.ObtenerTasaIvaAsync();
        if (!vigente.Success) TempData[Alerta.Error] = vigente.Message;
        return View(new CajaViewModel { TasaEstimada = vigente.Data, ParaLlevar = true, Categorias = categorias ?? [], MetodosPago = metodos ?? [] });
    }

    /// <summary>
    /// Registra la venta para llevar. Si sale bien, el aviso viaja en TempData al listado de ventas y la respuesta
    /// trae la direccion a la que vuelve la pantalla.
    /// </summary>
    [HttpPost("para-llevar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarParaLlevar([FromBody] ParaLlevarBody body)
    {
        var lineas = (body.Lineas ?? []).Select(l => (l.IdProducto, l.Cantidad));
        var (success, message, data, _, conflicto) = await ventasApiService.RegistrarParaLlevarAsync(
            body.ClaveIdempotencia, body.IdMetodoPago, body.TotalEsperado, lineas);

        string? redirect = null;
        if (success && data is not null)
        {
            var resumen = data.TasaIvaAplicada is { } tasa && data.Subtotal is { } subtotal && data.ImporteIva is { } iva
                ? $"Subtotal {CuentaViewModel.Dinero(subtotal)} · IVA ({tasa.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("es-CR"))}%) {CuentaViewModel.Dinero(iva)} · Total con IVA"
                : "Sin desglose de IVA registrado. Total";
            TempData[Alerta.Exito] = $"Venta para llevar {data.Folio} · {resumen} {CuentaViewModel.Dinero(data.Total)} con {data.MetodoPago}";
            redirect = Url.Action("Index", "VentasListado");
        }
        return Json(new { success, message, data, redirect, conflicto });
    }

    [HttpPost("cuenta/{idReserva:int}/cerrar-sin-cobro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSinCobro(int idReserva, [FromBody] CerrarSinCobroBody body)
    {
        var (success, message, data, cuenta, conflicto) = await ventasApiService.CerrarSinCobroAsync(idReserva, body.Motivo, body.Version);
        return Json(new { success, message, data, cuenta, conflicto });
    }

    private ContentResult ErrorParcial(string mensaje)
    {
        Response.StatusCode = StatusCodes.Status409Conflict;
        return Content(
            $"<div class=\"alert alert-danger mb-0\" role=\"alert\">{System.Net.WebUtility.HtmlEncode(mensaje)}</div>",
            "text/html");
    }

    public record LineaBody(int IdProducto, int Cantidad);
    public record PedidoBody(Guid ClaveIdempotencia, List<LineaBody>? Lineas, decimal? TotalEsperado);
    public record AnularLineaBody(int? Cantidad, string? Motivo);
    public record CobrarBody(int IdMetodoPago, decimal TotalEsperado, string? Version);
    public record CerrarSinCobroBody(string? Motivo, string? Version);
    public record ParaLlevarBody(Guid ClaveIdempotencia, int IdMetodoPago, decimal TotalEsperado, List<LineaBody>? Lineas);
}