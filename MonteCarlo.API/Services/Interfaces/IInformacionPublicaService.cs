using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

/// <summary>Información institucional de lectura que puede consultarse sin autenticación.</summary>
public interface IInformacionPublicaService
{
    Task<Result<HorariosPublicosResponse>> ObtenerHorariosAsync();
}
