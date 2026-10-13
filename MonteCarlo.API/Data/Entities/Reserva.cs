namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Entidad central del sistema de reservas.
/// </summary>
public class Reserva
{
    public int IdReserva { get; set; }

    /// <summary>Codigo unico generado por la aplicacion; permite consultar/cancelar sin login. Null en walk-ins (EsWalkIn): no hay cliente que lo reciba.</summary>
    public string? CodigoReserva { get; set; }

    /// <summary>En un walk-in (EsWalkIn), apunta al cliente centinela "Walk-in" sembrado en BD, no a una persona real.</summary>
    public int IdCliente { get; set; }

    public int IdEstadoReserva { get; set; }
    public DateOnly FechaReserva { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public int CantidadPersonas { get; set; }

    /// <summary>Marca las reservas que ninguna mesa individual pudo cubrir automaticamente (HU-MES-005/006).</summary>
    public bool RequiereAsignacionManual { get; set; }

    /// <summary>Cliente sentado directamente por el admin sin reserva previa (HU-OPE-004); no tiene CodigoReserva y su IdCliente es el centinela "Walk-in".</summary>
    public bool EsWalkIn { get; set; }

    /// <summary>Momento en que el cliente fue atendido (HU-OPE-003).</summary>
    public DateTime? FechaAtencion { get; set; }

    /// <summary>Cancelacion por cliente, administrativa, por cierre administrativo o por expiracion automatica.</summary>
    public string? MotivoCancelacion { get; set; }

    /// <summary>Solo si MotivoCancelacion corresponde a un cierre administrativo.</summary>
    public int? IdCierreEventual { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public EstadoReserva EstadoReserva { get; set; } = null!;
    public CierreEventual? CierreEventual { get; set; }
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    public ICollection<ReservaMesa> ReservaMesas { get; set; } = new List<ReservaMesa>();
}
