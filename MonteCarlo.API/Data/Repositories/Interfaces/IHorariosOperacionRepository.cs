using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IHorariosOperacionRepository
{
    Task<List<HorarioOperacion>> ObtenerTodosAsync();
    Task<HorarioOperacion?> ObtenerPorDiaAsync(byte diaSemana);
    Task<List<HorarioOperacion>> ObtenerPorDiasAsync(IEnumerable<byte> diasSemana);
    void Agregar(HorarioOperacion horarioOperacion);
    Task GuardarCambiosAsync();

    /// <summary>
    /// Vuelve a consultar los cierres fijos bajo una transaccion serializable y,
    /// si no existen, persiste todos los cambios rastreados (horarios e historial)
    /// en una sola escritura atomica.
    /// </summary>
    Task<List<byte>> ValidarCierresYGuardarCambiosAsync(IEnumerable<byte> diasSemana);
}
