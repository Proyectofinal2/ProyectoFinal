using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>
/// Cuenta abierta de la mesa y cobro (HU-CON-002). Lo opera cualquier administrador (General y Regular).
/// </summary>
[ApiController]
[Route("api")]
[Authorize(Roles = "General,Regular")]
public class VentasController(IVentasService ventasService) : ControllerBase
{
    /// <summary>Id de la cuenta autenticada (claim del token JWT).</summary>
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>La cuenta de la reserva con sus pedidos y lineas, el total y lo que se puede hacer con ella. Vacia si no hay pedidos.</summary>
    [HttpGet("ventas/cuentas/reserva/{idReserva:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerCuenta(int idReserva) =>
        this.Responder(await ventasService.ObtenerCuentaAsync(idReserva));

    /// <summary>
    /// Registra un pedido. El primero abre la cuenta y registra la llegada si faltaba. El precio se congela al agregarlo;
    /// reenviar la misma clave de idempotencia no duplica el pedido. 409 si la reserva no admite consumo.
    /// </summary>
    [HttpPost("ventas/cuentas/reserva/{idReserva:int}/pedidos")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AgregarPedido(int idReserva, [FromBody] AgregarPedidoRequest request) =>
        this.Responder(await ventasService.AgregarPedidoAsync(idReserva, request, IdUsuarioAutenticado));

    /// <summary>Quita una linea (o parte de ella) de una cuenta abierta; queda anulada con quien y cuando.</summary>
    [HttpPost("ventas/lineas/{idDetalle:int}/anular")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AnularLinea(int idDetalle, [FromBody] AnularLineaRequest request) =>
        this.Responder(await ventasService.AnularLineaAsync(idDetalle, request, IdUsuarioAutenticado));

    /// <summary>
    /// Cobra la cuenta con el total que vio la pantalla: finaliza la reserva y libera la mesa.
    /// 409 (con la cuenta actual) si el total cambio.
    /// </summary>
    [HttpPost("ventas/cuentas/reserva/{idReserva:int}/cobrar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cobrar(int idReserva, [FromBody] CobrarRequest request) =>
        this.Responder(await ventasService.CobrarAsync(idReserva, request, IdUsuarioAutenticado));

    /// <summary>Cierra la reserva sin consumo (total 0, sin metodo de pago). Solo con la cuenta vacia.</summary>
    [HttpPost("ventas/cuentas/reserva/{idReserva:int}/liberar-sin-consumo")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LiberarSinConsumo(int idReserva) =>
        this.Responder(await ventasService.LiberarSinConsumoAsync(idReserva, IdUsuarioAutenticado));

    /// <summary>Metodos de pago activos.</summary>
    [HttpGet("metodos-pago")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerMetodosPago() =>
        this.Responder(await ventasService.ObtenerMetodosPagoAsync());

    /// <summary>Catalogo vendible (producto disponible y categoria activa) agrupado por categoria.</summary>
    [HttpGet("productos/venta")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerCatalogoVenta() =>
        this.Responder(await ventasService.ObtenerCatalogoAsync());
}
