namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Dias de la semana marcados como cierre recurrente (HU-CFG-002).
/// Al eliminarse, la fila se borra; la accion queda en HistorialCierre.
/// </summary>
public class CierreFijo
{
    public int IdCierreFijo { get; set; }

    /// <summary>0 = Domingo ... 6 = Sabado.</summary>
    public byte DiaSemana { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }

    public Usuario UsuarioCreacion { get; set; } = null!;
}
