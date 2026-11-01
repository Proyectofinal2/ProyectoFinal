using System.Globalization;
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

    public Task<(bool Success, string Message, List<CierreEventualApiResponse>? Data)> ObtenerCierresEventualesAsync() =>
        EjecutarAsync<List<CierreEventualApiResponse>>(
            () => httpClient.GetAsync("cierres-eventuales"),
            "Cierres eventuales obtenidos.",
            "No se pudieron obtener los cierres eventuales. Intenta de nuevo.");

    public Task<(bool Success, string Message, CierreEventualApiResponse? Data)> RegistrarCierreEventualAsync(
        DateOnly fecha, string motivo, bool cancelarReservas) =>
        EjecutarAsync<CierreEventualApiResponse>(
            () => httpClient.PostAsJsonAsync("cierres-eventuales", new { fecha, motivo, cancelarReservas }),
            "El cierre fue registrado.",
            "No se pudo registrar el cierre. Intenta de nuevo.");

    public Task<(bool Success, string Message, CierreEventualApiResponse? Data)> EliminarCierreEventualAsync(int id) =>
        EjecutarAsync<CierreEventualApiResponse>(
            () => httpClient.DeleteAsync($"cierres-eventuales/{id}"),
            "El cierre fue quitado.",
            "No se pudo quitar el cierre. Intenta de nuevo.");

    public Task<(bool Success, string Message, List<HistorialCierreApiResponse>? Data)> ObtenerHistorialCierresAsync(
        string? tipo, string? accion, DateOnly? desde, DateOnly? hasta)
    {
        var filtros = new List<string>();
        if (!string.IsNullOrWhiteSpace(tipo)) filtros.Add($"tipo={Uri.EscapeDataString(tipo)}");
        if (!string.IsNullOrWhiteSpace(accion)) filtros.Add($"accion={Uri.EscapeDataString(accion)}");
        if (desde.HasValue) filtros.Add($"desde={desde.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        if (hasta.HasValue) filtros.Add($"hasta={hasta.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

        return EjecutarAsync<List<HistorialCierreApiResponse>>(
            () => httpClient.GetAsync("historial-cierres" + (filtros.Count > 0 ? "?" + string.Join("&", filtros) : "")),
            "Historial obtenido.",
            "No se pudo obtener el historial. Intenta de nuevo.");
    }

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

    public Task<(bool Success, string Message, List<HorarioOperacionApiResponse>? Data)> GuardarHorariosOperacionLoteAsync(
        IEnumerable<HorarioOperacionDiaViewModel> horarios) =>
        EjecutarAsync<List<HorarioOperacionApiResponse>>(
            () => httpClient.PutAsJsonAsync("horarios-operacion/lote", new
            {
                horarios = horarios.Select(h => new { h.DiaSemana, h.HoraApertura, h.HoraCierre })
            }),
            "Los horarios de operacion fueron guardados.",
            "No se pudieron guardar los horarios de operacion. Intenta de nuevo.");

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
