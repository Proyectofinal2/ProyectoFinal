using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IHorariosOperacionRepository
{
    Task<List<HorarioOperacion>> ObtenerTodosAsync();
    Task<HorarioOperacion?> ObtenerPorDiaAsync(byte diaSemana);
    void Agregar(HorarioOperacion horarioOperacion);
    Task GuardarCambiosAsync();
}
