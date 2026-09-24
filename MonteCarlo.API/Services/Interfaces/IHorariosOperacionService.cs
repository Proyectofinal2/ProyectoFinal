using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IHorariosOperacionService
{
    Task<Result<List<HorarioOperacionResponse>>> ObtenerAsync();
    Task<Result<HorarioOperacionResponse>> GuardarAsync(int? diaSemana, TimeOnly? horaApertura, TimeOnly? horaCierre);
}
