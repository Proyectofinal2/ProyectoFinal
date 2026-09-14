namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Fechas puntuales cerradas (evento, mantenimiento, etc.) - HU-CFG-003.
/// Estas filas se conservan siempre para el reporte de impacto (HU-REP-002).
/// </summary>
public class CierreEventual
{
    public int IdCierreEventual { get; set; }
    public DateOnly Fecha { get; set; }
    public string Motivo { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }

    public Usuario UsuarioCreacion { get; set; } = null!;
    public ICollection<Reserva> ReservasCanceladas { get; set; } = new List<Reserva>();
    public ICollection<HistorialCierre> HistorialCierres { get; set; } = new List<HistorialCierre>();
}
