namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Administrador del modulo administrativo. Rol: 'General' o 'Regular'.
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string CorreoElectronico { get; set; } = null!;
    /// <summary>Correo personal para recuperación de contraseña (HU-AUT-003). Opcional.</summary>
    public string? CorreoPersonal { get; set; }
    public string Rol { get; set; } = "Regular";
    public string ContrasenaHash { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; }

    /// <summary>NULL = no bloqueado.</summary>
    public DateTime? FechaBloqueoHasta { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ICollection<CierreFijo> CierresFijos { get; set; } = new List<CierreFijo>();
    public ICollection<CierreEventual> CierresEventuales { get; set; } = new List<CierreEventual>();
    public ICollection<HistorialCierre> HistorialCierres { get; set; } = new List<HistorialCierre>();
    public ICollection<ConfiguracionSistema> ConfiguracionesModificadas { get; set; } = new List<ConfiguracionSistema>();
    public ICollection<TokenRecuperacionContrasena> TokensRecuperacion { get; set; } = new List<TokenRecuperacionContrasena>();
    public ICollection<Venta> VentasRegistradas { get; set; } = new List<Venta>();
}
