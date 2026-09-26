using System.Globalization;
using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

public class ReservasApiService(HttpClient httpClient, ILogger<ReservasApiService> logger)
    : IReservasApiService
{
    public Task<(bool Success, string Message, DisponibilidadFechaApiResponse? Data)> ObtenerDisponibilidadAsync(
        int cantidadPersonas, DateOnly fecha) =>
        EjecutarAsync<DisponibilidadFechaApiResponse>(
            () => httpClient.GetAsync($"reservas/disponibilidad?cantidadPersonas={cantidadPersonas}&fecha={fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"),
            "Disponibilidad obtenida.",
            "No se pudo consultar la disponibilidad.");

    public Task<(bool Success, string Message, DisponibilidadMesApiResponse? Data)> ObtenerDisponibilidadMesAsync(
        int cantidadPersonas, int anio, int mes) =>
        EjecutarAsync<DisponibilidadMesApiResponse>(
            () => httpClient.GetAsync($"reservas/disponibilidad/mes?cantidadPersonas={cantidadPersonas}&anio={anio}&mes={mes}"),
            "Disponibilidad mensual obtenida.",
            "No se pudo consultar la disponibilidad mensual.");

    public Task<(bool Success, string Message, ReservaApiResponse? Data)> ObtenerPorCodigoAsync(string codigo) =>
        EjecutarAsync<ReservaApiResponse>(
            () => httpClient.GetAsync($"reservas/{Uri.EscapeDataString(codigo)}"),
            "Reserva encontrada.",
            "No se pudo consultar la reserva. Intenta de nuevo.");

    public Task<(bool Success, string Message, ReservaApiResponse? Data)> CancelarAsync(string codigo) =>
        EjecutarAsync<ReservaApiResponse>(
            () => httpClient.PostAsync($"reservas/{Uri.EscapeDataString(codigo)}/cancelar", null),
            "Tu reserva fue cancelada.",
            "No se pudo cancelar la reserva. Intenta de nuevo.");

    private async Task<(bool Success, string Message, T? Data)> EjecutarAsync<T>(
        Func<Task<HttpResponseMessage>> llamada, string mensajeExito, string mensajeError)
    {
        try
        {
            var response = await llamada();
            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<T>(response, mensajeExito, mensajeError);
            return (success, message, data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Error de conexion con la API: {Message}", ex.Message);
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", default);
        }
        catch (Exception ex)
        {
            logger.LogError("Error inesperado en reservas: {Message}", ex.Message);
            return (false, "Error inesperado. Intenta de nuevo.", default);
        }
    }
}