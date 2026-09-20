using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IReservasRepository
{
    /// Reserva con cliente, mesa y estado cargados (entidad rastreada).
    Task<Reserva?> ObtenerPorCodigoAsync(string codigo);

    Task<EstadoReserva?> ObtenerEstadoAsync(string nombre);

    Task GuardarCambiosAsync();
}
