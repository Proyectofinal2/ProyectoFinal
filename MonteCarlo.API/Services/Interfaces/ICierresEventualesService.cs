using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface ICierresEventualesService
{
    /// Cierres eventuales activos de hoy en adelante.
    Task<Result<List<CierreEventualResponse>>> ObtenerAsync();

    /// Registra el cierre. Si hay reservas en la fecha exige confirmar su cancelacion
    /// y las cancela liberando sus mesas.
    Task<Result<CierreEventualResponse>> RegistrarAsync(DateOnly fecha, string motivo, bool cancelarReservas, int idUsuario);

    /// Quita el cierre (la fila se conserva como inactiva). Las reservas ya canceladas no se restablecen.
    Task<Result<CierreEventualResponse>> EliminarAsync(int idCierreEventual, int idUsuario);
}
