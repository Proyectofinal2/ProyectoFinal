using MonteCarlo.WEB.Models.Reservas;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IReservasApiService
{
    Task<(bool Success, string Message, ReservaApiResponse? Data)> ObtenerPorCodigoAsync(string codigo);

    Task<(bool Success, string Message, ReservaApiResponse? Data)> CancelarAsync(string codigo);
}
