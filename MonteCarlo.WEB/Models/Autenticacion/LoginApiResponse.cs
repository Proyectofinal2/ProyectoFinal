namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>
/// Respuesta de la API después de un login exitoso.
/// </summary>
public class LoginApiResponse
{
    /// <summary>ID del usuario autenticado.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Nombre completo del usuario.</summary>
    public string NombreCompleto { get; set; } = null!;

    /// <summary>Correo electrónico del usuario.</summary>
    public string CorreoElectronico { get; set; } = null!;

    /// <summary>Rol del usuario ('General' o 'Regular').</summary>
    public string Rol { get; set; } = null!;

    /// <summary>Token JWT para autenticación.</summary>
    public string Token { get; set; } = null!;

    /// <summary>Segundos hasta que el token expire.</summary>
    public int ExpiresIn { get; set; }
}
