namespace MonteCarlo.API.Data.Entities;

/// Dias de la semana marcados como cierre recurrente (HU-CFG-002).
/// Al quitarse, la fila no se borra: queda con Activo = false y la accion queda en HistorialCierre.
public class CierreFijo
{
    public int IdCierreFijo { get; set; }

    /// 0 = Domingo ... 6 = Sabado.
    public byte DiaSemana { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int IdUsuarioCreacion { get; set; }

    /// False cuando el cierre fue quitado; la fila se conserva como registro.
    public bool Activo { get; set; } = true;
    public DateTime? FechaEliminacion { get; set; }
    public int? IdUsuarioEliminacion { get; set; }

    public Usuario UsuarioCreacion { get; set; } = null!;
    public Usuario? UsuarioEliminacion { get; set; }
}
