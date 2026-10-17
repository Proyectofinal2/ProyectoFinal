using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IReservasService
{
    Task<Result<DisponibilidadFechaResponse>> ObtenerDisponibilidadAsync(int cantidadPersonas, DateOnly fecha);
    Task<Result<DisponibilidadMesResponse>> ObtenerDisponibilidadMesAsync(int cantidadPersonas, int anio, int mes);
    Task<Result<ReservaResponse>> ObtenerPorCodigoAsync(string codigo);
    Task<Result<List<ReservaDelDiaResponse>>> ObtenerDelDiaAsync(DateOnly fecha);

    /// <summary>HU-OPE-005: busca reservas por nombre o teléfono del cliente desde hoy en adelante.</summary>
    Task<Result<List<ReservaDelDiaResponse>>> BuscarAsync(string termino);

    Task<Result<ReservaResponse>> CancelarAsync(string codigo);
    Task<Result<ReservaResponse>> CrearAsync(CrearReservaRequest request);
    Task<Result<AsignacionSugeridaResponse>> SugerirAsignacionPorReservaAsync(int idReserva);
    Task<Result<AsignacionMesaResponse>> AsignarMesaAsync(int idReserva, List<int> idsMesa);
    Task<Result<AsignacionSugeridaResponse>> SugerirWalkInAsync(int cantidadPersonas);
    Task<Result<ReservaWalkInResponse>> CrearWalkInAsync(CrearReservaWalkInRequest request);
    Task<Result> LiberarMesaAsync(int idMesa);

    /// <summary>HU-OPE-003: registra la llegada del cliente (FechaAtencion).</summary>
    Task<Result> RegistrarAtencionAsync(string codigo);

    /// <summary>HU-OPE-003: marca la reserva como No Show y libera la mesa.</summary>
    Task<Result> RegistrarNoShowAsync(string codigo);

    /// <summary>HU-OPE-002: Cambia el estado de una reserva validando las transiciones permitidas.</summary>
    Task<Result> CambiarEstadoAsync(string codigo, string nuevoEstado);
}