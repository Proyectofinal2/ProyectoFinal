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
    public Task<(bool Success, string Message, ConfiguracionGeneralApiResponse? Data)> ObtenerConfiguracionGeneralAsync() =>
        EjecutarAsync<ConfiguracionGeneralApiResponse>(
            () => httpClient.GetAsync("configuracion"),
            "Configuración obtenida.",
            "No se pudo obtener la configuración. Intenta de nuevo.");

    public Task<(bool Success, string Message, ConfiguracionGeneralApiResponse? Data)> ActualizarConfiguracionGeneralAsync(
        IEnumerable<(string Clave, string Valor)> cambios) =>
        EjecutarAsync<ConfiguracionGeneralApiResponse>(
            () => httpClient.PutAsJsonAsync("configuracion", new
            {
                parametros = cambios.Select(c => new { clave = c.Clave, valor = c.Valor })
            }),
            "Los cambios fueron guardados.",
            "No se pudieron guardar los cambios. Intenta de nuevo.");

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

            var resultado = await ApiResponseHelper.ParseAsync<T>(
                response, mensajeExito, mensajeError);

            return (resultado.Success, resultado.Message, resultado.Data);
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
