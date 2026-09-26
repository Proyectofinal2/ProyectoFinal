using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IReservasService
{
    Task<Result<DisponibilidadFechaResponse>> ObtenerDisponibilidadAsync(int cantidadPersonas, DateOnly fecha);

    Task<Result<DisponibilidadMesResponse>> ObtenerDisponibilidadMesAsync(int cantidadPersonas, int anio, int mes);

    Task<Result<ReservaResponse>> ObtenerPorCodigoAsync(string codigo);

    /// <summary>HU-RES-005: cancela una reserva Pendiente o Confirmada y libera su mesa.</summary>
    Task<Result<ReservaResponse>> CancelarAsync(string codigo);

    /// <summary>
    /// HU-RES-003: registra una reserva nueva. Aplica RN-01 y RN-03 si la
    /// cantidad de personas está dentro del umbral de confirmación automática,
    /// o RN-02 si lo supera. Genera el código único y devuelve los detalles.
    /// </summary>
    Task<Result<ReservaResponse>> CrearAsync(CrearReservaRequest request);
}