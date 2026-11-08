using MonteCarlo.WEB.Models.InformacionPublica;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IInformacionPublicaApiService
{
    Task<(bool Success, string Message, HorariosPublicosApiResponse? Data)> ObtenerHorariosAsync();
}
