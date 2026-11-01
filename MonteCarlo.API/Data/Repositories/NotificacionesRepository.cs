using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class NotificacionesRepository(MonteCarloDbContext context) : INotificacionesRepository
{
    public void Agregar(Notificacion notificacion) => context.Notificaciones.Add(notificacion);

    public Task<List<Notificacion>> ObtenerPendientesAsync(int limite) =>
        context.Notificaciones
            .Include(n => n.Reserva).ThenInclude(r => r.Cliente)
            .Include(n => n.Reserva).ThenInclude(r => r.EstadoReserva)
            .Where(n => n.EstadoEnvio == "Pendiente")
            .OrderBy(n => n.IdNotificacion)
            .Take(limite)
            .ToListAsync();

    public Task<List<Reserva>> ObtenerReservasSinRecordatorioAsync(DateOnly desde, DateOnly hasta) =>
        context.Reservas
            .Include(r => r.Cliente)
            .Where(r => r.FechaReserva >= desde && r.FechaReserva <= hasta
                        && r.EstadoReserva.Nombre == "Confirmada"
                        && !r.EsWalkIn
                        && r.FechaAtencion == null
                        && r.Cliente.CorreoElectronico != null
                        && !r.Notificaciones.Any(n => n.TipoNotificacion == "Recordatorio"))
            .ToListAsync();

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
