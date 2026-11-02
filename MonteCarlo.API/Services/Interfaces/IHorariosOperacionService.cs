using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IHorariosOperacionService
{
    Task<Result<List<HorarioOperacionResponse>>> ObtenerAsync();
    Task<Result<HorarioOperacionResponse>> GuardarAsync(
        int? diaSemana, TimeOnly? horaApertura, TimeOnly? horaCierre, int idUsuario);
    Task<Result<List<HorarioOperacionResponse>>> GuardarLoteAsync(
        IEnumerable<GuardarHorarioOperacionLoteItemRequest>? horarios, int idUsuario);
}
