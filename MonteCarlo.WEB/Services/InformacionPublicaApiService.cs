using MonteCarlo.WEB.Models.InformacionPublica;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Services;

/// <summary>Cliente HTTP para la información institucional pública de la API.</summary>
public class InformacionPublicaApiService(HttpClient httpClient, ILogger<InformacionPublicaApiService> logger)
    : IInformacionPublicaApiService
{
    public async Task<(bool Success, string Message, HorariosPublicosApiResponse? Data)> ObtenerHorariosAsync()
    {
        try
        {
            var response = await httpClient.GetAsync("informacion-publica/horarios");
            var resultado = await ApiResponseHelper.ParseAsync<HorariosPublicosApiResponse>(
                response,
                "Horarios de atención obtenidos.",
                "No se pudieron obtener los horarios de atención.");

            return (resultado.Success, resultado.Message, resultado.Data);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error de conexión al obtener los horarios públicos.");
            return (false, "No se pudieron obtener los horarios de atención.", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al obtener los horarios públicos.");
            return (false, "No se pudieron obtener los horarios de atención.", null);
        }
    }
}
