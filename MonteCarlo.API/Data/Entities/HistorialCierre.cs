namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Log de auditoria de acciones sobre cierres fijos y eventuales (HU-CFG-004).
/// Es independiente de CierreFijo/CierreEventual para que la accion "Eliminado"
/// quede registrada aun despues de borrar la fila de CierreFijo.
/// </summary>
public class HistorialCierre
{
    public int IdHistorialCierre { get; set; }

    /// <summary>Fijo / Eventual.</summary>
    public string TipoCierre { get; set; } = null!;

    /// <summary>Registrado / Eliminado.</summary>
    public string Accion { get; set; } = null!;

    /// <summary>Fecha y hora en que se realizo la accion.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Administrador responsable.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Aplica principalmente a cierres eventuales.</summary>
    public string? Motivo { get; set; }

    /// <summary>Aplica si TipoCierre es Fijo.</summary>
    public byte? DiaSemana { get; set; }

    /// <summary>Aplica si TipoCierre es Eventual.</summary>
    public DateOnly? FechaCierre { get; set; }

    /// <summary>Referencia opcional, solo para cierres eventuales.</summary>
    public int? IdCierreEventual { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public CierreEventual? CierreEventual { get; set; }
}
