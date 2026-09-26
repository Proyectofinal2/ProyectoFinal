using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IReservasRepository
{
    /// <summary>Reserva con cliente, mesa y estado cargados (entidad rastreada).</summary>
    Task<Reserva?> ObtenerPorCodigoAsync(string codigo);

    Task<EstadoReserva?> ObtenerEstadoAsync(string nombre);

    Task<List<byte>> ObtenerDiasCierreFijoAsync();
    Task<List<HorarioOperacion>> ObtenerHorariosOperacionAsync();

    /// <summary>Verifica si ya existe una reserva con ese código (evita duplicados).</summary>
    Task<bool> ExisteCodigoAsync(string codigo);

    /// <summary>Agrega una nueva reserva al contexto (HU-RES-003).</summary>
    void Agregar(Reserva reserva);

    Task GuardarCambiosAsync();
}