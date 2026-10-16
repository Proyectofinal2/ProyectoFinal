using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IHistorialCierresService
{
    /// Quien, cuando y por que se modifico la disponibilidad (cierres fijos y eventuales).
    Task<Result<List<HistorialCierreResponse>>> ObtenerAsync(string? tipo, string? accion, DateOnly? desde, DateOnly? hasta);
}
