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

    /// <summary>HU-MES-005/006: sugiere la mejor asignación de mesa para una reserva existente, sin aplicarla.</summary>
    Task<Result<AsignacionSugeridaResponse>> SugerirAsignacionPorReservaAsync(int idReserva);

    /// <summary>HU-MES-005/006: aplica 1 o 2 mesas elegidas por el admin, validando capacidad y solapamiento.</summary>
    Task<Result<AsignacionMesaResponse>> AsignarMesaAsync(int idReserva, List<int> idsMesa);

    /// <summary>HU-OPE-004: variante de BuscarMejorAsignacion para "ahora", sin una Reserva todavía creada.</summary>
    Task<Result<AsignacionSugeridaResponse>> SugerirWalkInAsync(int cantidadPersonas);

    /// <summary>HU-OPE-004: sienta un cliente sin reserva previa en 1 o 2 mesas elegidas por el admin.</summary>
    Task<Result<ReservaWalkInResponse>> CrearWalkInAsync(CrearReservaWalkInRequest request);

    /// <summary>HU-OPE-004 (gap #5): libera una mesa ocupada por abandono sin consumo.</summary>
    Task<Result> LiberarMesaAsync(int idMesa);
}