using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IMesasRepository
{
    /// <summary>
    /// Devuelve las mesas disponibles con capacidad suficiente para la
    /// cantidad de personas, ordenadas de menor a mayor capacidad (RN-03).
    /// </summary>
    Task<List<Mesa>> ObtenerDisponiblesPorCapacidadAsync(int cantidadPersonas);

    /// <summary>
    /// Devuelve las reservas activas (Pendiente, Confirmada) de una mesa
    /// en una fecha determinada, para validar solapamiento por horario.
    /// </summary>
    Task<List<Reserva>> ObtenerReservasActivasPorMesaYFechaAsync(int idMesa, DateOnly fecha);
}