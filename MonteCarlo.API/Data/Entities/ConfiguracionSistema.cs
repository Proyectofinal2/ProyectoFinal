namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Tabla clave/valor generica de parametros del sistema (HU-CFG-001).
/// La llave primaria es Clave.
/// </summary>
public class ConfiguracionSistema
{
    public string Clave { get; set; } = null!;
    public string Valor { get; set; } = null!;
    public DateTime FechaModificacion { get; set; }
    public int? IdUsuarioModificacion { get; set; }

    public Usuario? UsuarioModificacion { get; set; }
}
