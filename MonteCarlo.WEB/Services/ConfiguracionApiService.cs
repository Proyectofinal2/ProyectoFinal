using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;


/// Consume los endpoints de configuración de la API (solo rol General).
/// HU-CFG-001: umbral de confirmación automática.
/// HU-CFG-002: cierres fijos semanales.
/// El Bearer token lo agrega AuthTokenHandler.

public class ConfiguracionApiService(HttpClient httpClient, ILogger<ConfiguracionApiService> logger)
    : IConfiguracionApiService
{
    public Task<(bool Success, string Message, UmbralApiResponse? Data)> ObtenerUmbralAsync() =>
        EjecutarAsync<UmbralApiResponse>(
            () => httpClient.GetAsync("configuracion/umbral-confirmacion"),
            "Umbral obtenido.",
            "No se pudo obtener el umbral. Intenta de nuevo.");

    public Task<(bool Success, string Message, UmbralApiResponse? Data)> ActualizarUmbralAsync(int umbral) =>
        EjecutarAsync<UmbralApiResponse>(
            () => httpClient.PutAsJsonAsync("configuracion/umbral-confirmacion", new { umbral }),
            "El umbral fue actualizado.",
            "No se pudo actualizar el umbral. Intenta de nuevo.");

    public Task<(bool Success, string Message, CierresFijosApiResponse? Data)> ObtenerCierresFijosAsync() =>
        EjecutarAsync<CierresFijosApiResponse>(
            () => httpClient.GetAsync("cierres-fijos"),
            "Cierres fijos obtenidos.",
            "No se pudieron obtener los cierres fijos. Intenta de nuevo.");

    public Task<(bool Success, string Message, CierresFijosApiResponse? Data)> ActualizarCierresFijosAsync(IEnumerable<int> dias) =>
        EjecutarAsync<CierresFijosApiResponse>(
            () => httpClient.PutAsJsonAsync("cierres-fijos", new { dias }),
            "Los cierres fijos fueron actualizados.",
            "No se pudieron actualizar los cierres fijos. Intenta de nuevo.");

    public Task<(bool Success, string Message, List<HorarioOperacionApiResponse>? Data)> ObtenerHorariosOperacionAsync() =>
        EjecutarAsync<List<HorarioOperacionApiResponse>>(
            () => httpClient.GetAsync("horarios-operacion"),
            "Horarios de operación obtenidos.",
            "No se pudieron obtener los horarios de operación. Intenta de nuevo.");

    public Task<(bool Success, string Message, HorarioOperacionApiResponse? Data)> GuardarHorarioOperacionAsync(
        int diaSemana, TimeOnly horaApertura, TimeOnly horaCierre) =>
        EjecutarAsync<HorarioOperacionApiResponse>(
            () => httpClient.PutAsJsonAsync("horarios-operacion", new { diaSemana, horaApertura, horaCierre }),
            "El horario de operación fue guardado.",
            "No se pudo guardar el horario de operación. Intenta de nuevo.");

    private async Task<(bool Success, string Message, T? Data)> EjecutarAsync<T>(
        Func<Task<HttpResponseMessage>> llamada, string mensajeExito, string mensajeError)
    {
        try
        {
            var response = await llamada();

            var (success, message, data, _) = await ApiResponseHelper.ParseAsync<T>(
                response, mensajeExito, mensajeError);

            return (success, message, data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", default);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado en configuración: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", default);
        }
    }
}
