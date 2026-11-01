namespace MonteCarlo.API.Data.Entities;

/// Fechas puntuales cerradas (evento, mantenimiento, etc.) - HU-CFG-003.
/// Estas filas se conservan siempre para el reporte de impacto (HU-REP-002).
public class CierreEventual
{
    public int IdCierreEventual { get; set; }
    public DateOnly Fecha { get; set; }
    public string Motivo { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }

    /// False cuando el cierre fue eliminado; la fila se conserva como registro.
    public bool Activo { get; set; } = true;
    public DateTime? FechaEliminacion { get; set; }
    public int? IdUsuarioEliminacion { get; set; }

    public Usuario UsuarioCreacion { get; set; } = null!;
    public Usuario? UsuarioEliminacion { get; set; }
    public ICollection<Reserva> ReservasCanceladas { get; set; } = new List<Reserva>();
    public ICollection<HistorialCierre> HistorialCierres { get; set; } = new List<HistorialCierre>();
}
