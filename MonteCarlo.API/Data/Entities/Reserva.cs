namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Entidad central del sistema de reservas.
/// </summary>
public class Reserva
{
    public int IdReserva { get; set; }

    /// <summary>Codigo unico generado por la aplicacion; permite consultar/cancelar sin login.</summary>
    public string CodigoReserva { get; set; } = null!;
    public int IdCliente { get; set; }

    /// <summary>NULL mientras esta Pendiente o requiere asignacion manual.</summary>
    public int? IdMesa { get; set; }
    public int IdEstadoReserva { get; set; }
    public DateOnly FechaReserva { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public int CantidadPersonas { get; set; }

    /// <summary>Marca las reservas que ninguna mesa individual pudo cubrir automaticamente (HU-MES-005/006).</summary>
    public bool RequiereAsignacionManual { get; set; }

    /// <summary>Momento en que el cliente fue atendido (HU-OPE-003).</summary>
    public DateTime? FechaAtencion { get; set; }

    /// <summary>Cancelacion por cliente, administrativa, por cierre administrativo o por expiracion automatica.</summary>
    public string? MotivoCancelacion { get; set; }

    /// <summary>Solo si MotivoCancelacion corresponde a un cierre administrativo.</summary>
    public int? IdCierreEventual { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Mesa? Mesa { get; set; }
    public EstadoReserva EstadoReserva { get; set; } = null!;
    public CierreEventual? CierreEventual { get; set; }
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
