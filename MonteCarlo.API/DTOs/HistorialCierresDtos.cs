namespace MonteCarlo.API.DTOs;

/// Accion registrada sobre un cierre fijo o eventual.
public class HistorialCierreResponse
{
    public int IdHistorialCierre { get; set; }

    /// Fecha y hora en que se hizo el cambio.
    public DateTime Fecha { get; set; }

    /// Fijo / Eventual.
    public string TipoCierre { get; set; } = null!;

    /// Registrado / Eliminado.
    public string Accion { get; set; } = null!;

    /// 0 = Domingo ... 6 = Sabado. Solo cierres fijos.
    public int? DiaSemana { get; set; }

    /// Fecha cerrada. Solo cierres eventuales.
    public DateOnly? FechaCierre { get; set; }

    public string? Motivo { get; set; }
    public string Usuario { get; set; } = null!;
}
