namespace MonteCarlo.API.Data.Entities;

public class Notificacion
{
    public int IdNotificacion { get; set; }
    public int IdReserva { get; set; }

    /// <summary>Confirmacion / Cancelacion / Recordatorio.</summary>
    public string TipoNotificacion { get; set; } = null!;

    /// <summary>Email / SMS.</summary>
    public string Canal { get; set; } = null!;
    public string Destinatario { get; set; } = null!;
    public string? Mensaje { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }

    /// <summary>Pendiente / Enviado / Fallido.</summary>
    public string EstadoEnvio { get; set; } = "Pendiente";
    public string? DetalleError { get; set; }

    public Reserva Reserva { get; set; } = null!;
}
