using MonteCarlo.WEB.Models.Ventas;

namespace MonteCarlo.WEB.Services.Interfaces;

/// <summary>
/// Cuenta abierta de la mesa y cobro (HU-CON-002). Cuando la API responde 409 porque la cuenta cambio,
/// <c>Cuenta</c> trae la cuenta actual para que la pantalla se refresque sin otra llamada.
/// </summary>
public interface IVentasApiService
{
    Task<(bool Success, string Message, CuentaApiResponse? Data)> ObtenerCuentaAsync(int idReserva);

    Task<(bool Success, string Message, CuentaApiResponse? Data, CuentaApiResponse? Cuenta)> AgregarPedidoAsync(
        int idReserva, Guid claveIdempotencia, List<(int IdProducto, int Cantidad)> lineas);

    Task<(bool Success, string Message, CuentaApiResponse? Data, CuentaApiResponse? Cuenta)> AnularLineaAsync(
        int idDetalle, int? cantidad, string? motivo);

    Task<(bool Success, string Message, CobroApiResponse? Data, CuentaApiResponse? Cuenta)> CobrarAsync(
        int idReserva, int idMetodoPago, decimal totalEsperado, string? version);

    Task<(bool Success, string Message, CobroApiResponse? Data, CuentaApiResponse? Cuenta)> LiberarSinConsumoAsync(int idReserva);

    Task<(bool Success, string Message, CobroApiResponse? Data, CuentaApiResponse? Cuenta)> CerrarSinCobroAsync(
        int idReserva, string? motivo, string? version);

    Task<(bool Success, string Message, List<MetodoPagoApiResponse>? Data)> ObtenerMetodosPagoAsync();

    Task<(bool Success, string Message, List<CategoriaVentaApiResponse>? Data)> ObtenerCatalogoVentaAsync();

    Task<(bool Success, string Message, List<VentaListadoApiResponse>? Data)> ObtenerListadoAsync(FiltroVentasViewModel filtro);

    Task<(bool Success, string Message, VentaDetalleApiResponse? Data)> ObtenerDetalleAsync(int idVenta);
}
