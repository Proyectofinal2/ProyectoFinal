using MonteCarlo.WEB.Models.Mesas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

/// Consume MesasController y los endpoints de walk-in de ReservasController en la API
/// (HU-MES-001/002/003/006, HU-OPE-004). El Bearer token lo agrega AuthTokenHandler.
public class MesasApiService(HttpClient httpClient, ILogger<MesasApiService> logger) : IMesasApiService
{
    public Task<(bool Success, string Message, List<MesaApiResponse>? Data)> ObtenerMesasAsync(bool incluirDeshabilitadas) =>
        EjecutarAsync<List<MesaApiResponse>>(
            () => httpClient.GetAsync($"mesas?incluirDeshabilitadas={incluirDeshabilitadas}"),
            "Mesas obtenidas.",
            "No se pudieron obtener las mesas. Intenta de nuevo.");

    public Task<(bool Success, string Message, SugerenciaWalkInApiResponse? Data)> ObtenerSugerenciaWalkInAsync(int cantidadPersonas) =>
        EjecutarAsync<SugerenciaWalkInApiResponse>(
            () => httpClient.GetAsync($"mesas/sugerencia-walkin?cantidadPersonas={cantidadPersonas}"),
            "Sugerencia calculada.",
            "No se pudo calcular la sugerencia. Intenta de nuevo.");

    public Task<(bool Success, string Message, ReservaWalkInApiResponse? Data)> CrearWalkInAsync(int cantidadPersonas, List<int> idsMesa) =>
        EjecutarAsync<ReservaWalkInApiResponse>(
            () => httpClient.PostAsJsonAsync("reservas/walkin", new { cantidadPersonas, idsMesa }),
            "Cliente sentado correctamente.",
            "No se pudo sentar al cliente. Intenta de nuevo.");

    public async Task<(bool Success, string Message)> LiberarMesaAsync(int idMesa)
    {
        var (success, message, _) = await EjecutarAsync<object>(
            () => httpClient.PutAsJsonAsync($"mesas/{idMesa}/liberar", new { }),
            "Mesa liberada.",
            "No se pudo liberar la mesa. Intenta de nuevo.");
        return (success, message);
    }

    public Task<(bool Success, string Message, MesaApiResponse? Data)> CrearMesaAsync(int numeroMesa, int capacidad, int posX, int posY) =>
        EjecutarAsync<MesaApiResponse>(
            () => httpClient.PostAsJsonAsync("mesas", new { numeroMesa, capacidad, posX, posY }),
            "Mesa creada.",
            "No se pudo crear la mesa. Intenta de nuevo.");

    public Task<(bool Success, string Message, MesaApiResponse? Data)> ActualizarCapacidadAsync(int idMesa, int capacidad) =>
        EjecutarAsync<MesaApiResponse>(
            () => httpClient.PutAsJsonAsync($"mesas/{idMesa}", new { capacidad }),
            "Capacidad actualizada.",
            "No se pudo actualizar la capacidad. Intenta de nuevo.");

    public Task<(bool Success, string Message, MesaApiResponse? Data)> ActualizarEstadoAsync(int idMesa, string estado) =>
        EjecutarAsync<MesaApiResponse>(
            () => httpClient.PutAsJsonAsync($"mesas/{idMesa}/estado", new { estado }),
            "Estado actualizado.",
            "No se pudo actualizar el estado. Intenta de nuevo.");

    public Task<(bool Success, string Message, MesaApiResponse? Data)> ActualizarPosicionAsync(int idMesa, int posX, int posY) =>
        EjecutarAsync<MesaApiResponse>(
            () => httpClient.PutAsJsonAsync($"mesas/{idMesa}/posicion", new { posX, posY }),
            "Posicion actualizada.",
            "No se pudo actualizar la posicion. Intenta de nuevo.");

    public async Task<(bool Success, string Message)> EliminarMesaAsync(int idMesa)
    {
        var (success, message, _) = await EjecutarAsync<object>(
            () => httpClient.DeleteAsync($"mesas/{idMesa}"),
            "Mesa eliminada.",
            "No se pudo eliminar la mesa. Intenta de nuevo.");
        return (success, message);
    }

    private async Task<(bool Success, string Message, T? Data)> EjecutarAsync<T>(
        Func<Task<HttpResponseMessage>> llamada, string mensajeExito, string mensajeError)
    {
        try
        {
            var response = await llamada();
            var resultado = await ApiResponseHelper.ParseAsync<T>(response, mensajeExito, mensajeError);
            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Error de conexión con la API: {ex.Message}");
            return (false, "No se pudo conectar con el servidor. Intenta de nuevo.", default);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error inesperado en mesas: {ex.Message}");
            return (false, "Error inesperado. Intenta de nuevo.", default);
        }
    }
}
