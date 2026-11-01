using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface INotificacionesRepository
{
    void Agregar(Notificacion notificacion);

    /// Notificaciones en estado Pendiente, con la reserva, su cliente y su estado, de la mas antigua a la mas reciente.
    Task<List<Notificacion>> ObtenerPendientesAsync(int limite);

    /// Reservas confirmadas entre las dos fechas, con correo del cliente y sin recordatorio registrado.
    Task<List<Reserva>> ObtenerReservasSinRecordatorioAsync(DateOnly desde, DateOnly hasta);

    Task GuardarCambiosAsync();
}
