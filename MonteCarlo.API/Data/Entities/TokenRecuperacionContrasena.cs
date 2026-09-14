namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Token de recuperacion de contrasena (HU-AUT-003).
/// </summary>
public class TokenRecuperacionContrasena
{
    public int IdToken { get; set; }
    public int IdUsuario { get; set; }

    /// <summary>Nunca se guarda el token en texto plano.</summary>
    public string TokenHash { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool Utilizado { get; set; }
    public DateTime? FechaUtilizado { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
