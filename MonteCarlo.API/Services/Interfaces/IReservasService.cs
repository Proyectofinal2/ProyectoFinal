using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IReservasService
{
    Task<Result<DisponibilidadFechaResponse>> ObtenerDisponibilidadAsync(int cantidadPersonas, DateOnly fecha);
    Task<Result<DisponibilidadMesResponse>> ObtenerDisponibilidadMesAsync(int cantidadPersonas, int anio, int mes);
    Task<Result<ReservaResponse>> ObtenerPorCodigoAsync(string codigo);

    /// HU-RES-005: cancela una reserva Pendiente o Confirmada y libera su mesa.
    Task<Result<ReservaResponse>> CancelarAsync(string codigo);
}
