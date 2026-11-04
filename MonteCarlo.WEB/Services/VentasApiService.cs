using System.Text.Json;
using MonteCarlo.WEB.Models.Ventas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

/// <summary>Consume VentasController de la API (HU-CON-002). El Bearer token lo agrega AuthTokenHandler.</summary>
public class VentasApiService(HttpClient httpClient, ILogger<VentasApiService> logger) : IVentasApiService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public async Task<(bool Success, string Message, CuentaApiResponse? Data)> ObtenerCuentaAsync(int idReserva)
    {
        var (success, message, data, _) = await EjecutarAsync<CuentaApiResponse>(
            () => httpClient.GetAsync($"ventas/cuentas/reserva/{idReserva}"),
            "Cuenta obtenida.", "No se pudo obtener la cuenta. Intenta de nuevo.");
        return (success, message, data);
    }

    public Task<(bool Success, string Message, CuentaApiResponse? Data, CuentaApiResponse? Cuenta)> AgregarPedidoAsync(
        int idReserva, Guid claveIdempotencia, List<(int IdProducto, int Cantidad)> lineas) =>
        EjecutarAsync<CuentaApiResponse>(
            () => httpClient.PostAsJsonAsync($"ventas/cuentas/reserva/{idReserva}/pedidos", new
            {
                claveIdempotencia,
                lineas = lineas.Select(l => new { l.IdProducto, l.Cantidad })
            }),
            "Pedido registrado.", "No se pudo registrar el pedido. Intenta de nuevo.");

    public Task<(bool Success, string Message, CuentaApiResponse? Data, CuentaApiResponse? Cuenta)> AnularLineaAsync(
        int idDetalle, int? cantidad, string? motivo) =>
        EjecutarAsync<CuentaApiResponse>(
            () => httpClient.PostAsJsonAsync($"ventas/lineas/{idDetalle}/anular", new { cantidad, motivo }),
            "Producto quitado de la cuenta.", "No se pudo quitar el producto. Intenta de nuevo.");

    public Task<(bool Success, string Message, CobroApiResponse? Data, CuentaApiResponse? Cuenta)> CobrarAsync(
        int idReserva, int idMetodoPago, decimal totalEsperado, string? version) =>
        EjecutarAsync<CobroApiResponse>(
            () => httpClient.PostAsJsonAsync($"ventas/cuentas/reserva/{idReserva}/cobrar", new { idMetodoPago, totalEsperado, version }),
            "Cuenta cobrada.", "No se pudo cobrar la cuenta. Intenta de nuevo.");

    public Task<(bool Success, string Message, CobroApiResponse? Data, CuentaApiResponse? Cuenta)> LiberarSinConsumoAsync(int idReserva) =>
        EjecutarAsync<CobroApiResponse>(
            () => httpClient.PostAsync($"ventas/cuentas/reserva/{idReserva}/liberar-sin-consumo", null),
            "Mesa liberada sin consumo.", "No se pudo liberar la mesa. Intenta de nuevo.");

    public async Task<(bool Success, string Message, List<MetodoPagoApiResponse>? Data)> ObtenerMetodosPagoAsync()
    {
        var (success, message, data, _) = await EjecutarAsync<List<MetodoPagoApiResponse>>(
            () => httpClient.GetAsync("metodos-pago"),
            "Metodos de pago obtenidos.", "No se pudieron obtener los metodos de pago. Intenta de nuevo.");
        return (success, message, data);
    }

    public async Task<(bool Success, string Message, List<CategoriaVentaApiResponse>? Data)> ObtenerCatalogoVentaAsync()
    {
        var (success, message, data, _) = await EjecutarAsync<List<CategoriaVentaApiResponse>>(
            () => httpClient.GetAsync("productos/venta"),
            "Menu obtenido.", "No se pudo obtener el menu. Intenta de nuevo.");
        return (success, message, data);
    }

    private async Task<(bool Success, string Message, T? Data, CuentaApiResponse? Cuenta)> EjecutarAsync<T>(
        Func<Task<HttpResponseMessage>> llamada, string mensajeExito, string mensajeError)
    {
        try
        {
            var response = await llamada();
            var resultado = await ApiResponseHelper.ParseAsync<T>(response, mensajeExito, mensajeError);

            // Un 409 por cuenta cambiada trae la cuenta actual en "data".
            CuentaApiResponse? cuenta = null;
            if (!resultado.Success && resultado.ErrorData is { ValueKind: JsonValueKind.Object } error
                && error.TryGetProperty("idReserva", out _))
            {
                cuenta = error.Deserialize<CuentaApiResponse>(Json);
            }

            return (resultado.Success, resultado.Message, resultado.Data, cuenta);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", default, null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado en ventas: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", default, null);
        }
    }

    public async Task<(bool Success, string Message, List<VentaListadoApiResponse>? Data)> ObtenerListadoAsync(FiltroVentasViewModel filtro)
    {
        var query = new List<string>();
        if (filtro.FechaInicio.HasValue) query.Add($"fechaInicio={filtro.FechaInicio.Value:yyyy-MM-dd}");
        if (filtro.FechaFin.HasValue) query.Add($"fechaFin={filtro.FechaFin.Value:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(filtro.TipoVenta)) query.Add($"tipoVenta={filtro.TipoVenta}");
        if (filtro.IdMetodoPago.HasValue) query.Add($"idMetodoPago={filtro.IdMetodoPago}");
        if (!string.IsNullOrEmpty(filtro.Estado)) query.Add($"estado={filtro.Estado}");

        var url = "ventas" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var (success, message, data, _) = await EjecutarAsync<List<VentaListadoApiResponse>>(
            () => httpClient.GetAsync(url),
            "Ventas obtenidas.", "No se pudieron obtener las ventas.");

        return (success, message, data);
    }

    public async Task<(bool Success, string Message, VentaDetalleApiResponse? Data)> ObtenerDetalleAsync(int idVenta)
    {
        var (success, message, data, _) = await EjecutarAsync<VentaDetalleApiResponse>(
            () => httpClient.GetAsync($"ventas/{idVenta}"),
            "Detalle obtenido.", "No se pudo obtener el detalle.");
        return (success, message, data);
    }
}
