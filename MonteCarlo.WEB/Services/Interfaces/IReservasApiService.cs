using MonteCarlo.WEB.Models.Reservas;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IReservasApiService
{
    Task<(bool Success, string Message, DisponibilidadFechaApiResponse? Data)> ObtenerDisponibilidadAsync(int cantidadPersonas, DateOnly fecha);
    Task<(bool Success, string Message, DisponibilidadMesApiResponse? Data)> ObtenerDisponibilidadMesAsync(int cantidadPersonas, int anio, int mes);
    Task<(bool Success, string Message, ReservaApiResponse? Data)> ObtenerPorCodigoAsync(string codigo);

    Task<(bool Success, string Message, ReservaApiResponse? Data)> CancelarAsync(string codigo);

    Task<(bool Success, string Message, ReservaApiResponse? Data)> CrearAsync(DatosReservaViewModel modelo);

}
