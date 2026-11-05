using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IVentasService
{
    /// <summary>La cuenta de la reserva (vacia si todavia no hay pedidos), con lo que se puede hacer con ella.</summary>
    Task<Result<CuentaResponse>> ObtenerCuentaAsync(int idReserva);

    /// <summary>Registra una ronda de pedido. El primero abre la cuenta; el precio se congela aqui.</summary>
    Task<Result<CuentaResponse>> AgregarPedidoAsync(int idReserva, AgregarPedidoRequest request, int idUsuario);

    /// <summary>Quita una linea (o parte de ella) de una cuenta abierta: queda anulada con quien y cuando.</summary>
    Task<Result<CuentaResponse>> AnularLineaAsync(int idDetalle, AnularLineaRequest request, int idUsuario);

    /// <summary>Cobra la cuenta abierta: finaliza la reserva y libera la mesa. 409 si el total cambio.</summary>
    Task<Result<CobroResponse>> CobrarAsync(int idReserva, CobrarRequest request, int idUsuario);

    /// <summary>
    /// Cierra sin cobrar una cuenta antigua con productos (el cliente se fue sin pagar): queda Abandonada con quien,
    /// cuando y por que, conserva su total y lineas, finaliza la reserva y libera la mesa. No es ingreso.
    /// </summary>
    Task<Result<CobroResponse>> CerrarSinCobroAsync(int idReserva, CerrarSinCobroRequest request, int idUsuario);

    /// <summary>Cierra la reserva sin consumo y libera la mesa, sin registrar una venta; solo con la cuenta vacia.</summary>
    Task<Result<CobroResponse>> LiberarSinConsumoAsync(int idReserva, int idUsuario);

    /// <summary>Libera la mesa de una reserva sentada hoy (cierre sin consumo); 400 si la mesa no tiene reserva sentada.</summary>
    Task<Result<CobroResponse>> LiberarSinConsumoPorMesaAsync(int idMesa, int idUsuario);

    Task<Result<List<MetodoPagoResponse>>> ObtenerMetodosPagoAsync();

    /// <summary>Productos disponibles con categoria activa, agrupados por categoria, para el selector.</summary>
    Task<Result<List<CategoriaVentaResponse>>> ObtenerCatalogoAsync();

    Task<Result<List<VentaListadoResponse>>> ObtenerListadoAsync(FiltroVentasRequest filtro);

    Task<Result<VentaDetalleResponse>> ObtenerDetalleAsync(int idVenta);
}
