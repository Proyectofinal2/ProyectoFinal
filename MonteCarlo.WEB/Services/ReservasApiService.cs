using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;


/// Consume los endpoints públicos de reservas de la API.
/// HU-RES-005: consultar y cancelar una reserva con su código.

public class ReservasApiService(HttpClient httpClient, ILogger<ReservasApiService> logger)
    : IReservasApiService
{
    public Task<(bool Success, string Message, ReservaApiResponse? Data)> ObtenerPorCodigoAsync(string codigo) =>
        EjecutarAsync(
            () => httpClient.GetAsync($"reservas/{Uri.EscapeDataString(codigo)}"),
            "Reserva encontrada.",
            "No se pudo consultar la reserva. Intenta de nuevo.");

    public Task<(bool Success, string Message, ReservaApiResponse? Data)> CancelarAsync(string codigo) =>
        EjecutarAsync(
            () => httpClient.PostAsync($"reservas/{Uri.EscapeDataString(codigo)}/cancelar", null),
            "Tu reserva fue cancelada.",
            "No se pudo cancelar la reserva. Intenta de nuevo.");

    private async Task<(bool Success, string Message, ReservaApiResponse? Data)> EjecutarAsync(
        Func<Task<HttpResponseMessage>> llamada, string mensajeExito, string mensajeError)
    {
        try
        {
            var response = await llamada();

            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<ReservaApiResponse>(
                response, mensajeExito, mensajeError);

            return (success, message, data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado en reservas: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", null);
        }
    }
}
