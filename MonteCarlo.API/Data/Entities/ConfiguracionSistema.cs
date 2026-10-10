namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Tabla clave/valor generica de parametros del sistema (HU-CFG-001).
/// La llave primaria es Clave.
/// </summary>
public class ConfiguracionSistema
{
    public string Clave { get; set; } = null!;
    public string Valor { get; set; } = null!;

    /// <summary>Nombre amigable para la pantalla de Parametros. Si es null, se muestra la Clave.</summary>
    public string? Etiqueta { get; set; }

    /// <summary>Texto de ayuda bajo el input en la pantalla de Parametros.</summary>
    public string? Descripcion { get; set; }

    public DateTime FechaModificacion { get; set; }
    public int? IdUsuarioModificacion { get; set; }

    public Usuario? UsuarioModificacion { get; set; }
}
