using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services.Interfaces;

public interface INotificacionesService
{
    /// Deja en cola un correo para el cliente de la reserva (Confirmacion, Cancelacion o Recordatorio).
    /// No guarda: se guarda junto con el cambio de la reserva. Si no hay correo o es un walk-in, no hace nada.
    void Encolar(Reserva reserva, string? correo, string tipoNotificacion);

    /// Encola los recordatorios que correspondan y envia los correos pendientes.
    /// Lo ejecuta el proceso automatico de notificaciones cada minuto.
    Task ProcesarAsync(CancellationToken cancellationToken = default);
}
