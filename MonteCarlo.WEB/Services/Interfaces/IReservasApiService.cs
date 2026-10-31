using MonteCarlo.WEB.Models.Reservas;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IReservasApiService
{
    Task<(bool Success, string Message, DisponibilidadFechaApiResponse? Data)> ObtenerDisponibilidadAsync(int cantidadPersonas, DateOnly fecha);
    Task<(bool Success, string Message, DisponibilidadMesApiResponse? Data)> ObtenerDisponibilidadMesAsync(int cantidadPersonas, int anio, int mes);
    Task<(bool Success, string Message, ReservaApiResponse? Data)> ObtenerPorCodigoAsync(string codigo);

    Task<(bool Success, string Message, ReservaApiResponse? Data)> CancelarAsync(string codigo);

    Task<(bool Success, string Message, ReservaApiResponse? Data)> CrearAsync(DatosReservaViewModel modelo);

    Task<(bool Success, string Message, List<ReservaDelDiaApiResponse>? Data)> ObtenerReservasDelDiaAsync(DateOnly fecha);

    Task<(bool Success, string Message, List<EstadoReservaReporteApiResponse>? Data)> ObtenerEstadosReporteAsync();

    Task<(bool Success, string Message, ReporteReservasApiResponse? Data)> ObtenerReporteAsync(
        DateOnly fechaInicio, DateOnly fechaFin, string? estado);

    Task<(bool Success, string Message, List<ReservaDelDiaApiResponse>? Data)> BuscarAsync(string termino);
    /// HU-OPE-003: registra la llegada del cliente.
    Task<(bool Success, string Message)> RegistrarAtencionAsync(string codigo);

    /// HU-OPE-003: marca la reserva como No Show.
    Task<(bool Success, string Message)> RegistrarNoShowAsync(string codigo);

    /// HU-OPE-002: Cambia el estado de la reserva.
    Task<(bool Success, string Message)> CambiarEstadoAsync(string codigo, string nuevoEstado);
}
