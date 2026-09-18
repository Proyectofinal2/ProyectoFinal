namespace MonteCarlo.API.DTOs;

/// <summary>
/// Respuesta exitosa de inicio de sesión.
/// Contiene los datos del usuario y un token JWT para autenticación posterior.
/// </summary>
public class LoginResponse
{
    /// <summary>ID del usuario autenticado.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Nombre completo del usuario.</summary>
    public string NombreCompleto { get; set; } = null!;

    /// <summary>Correo electrónico del usuario.</summary>
    public string CorreoElectronico { get; set; } = null!;

    /// <summary>Rol del usuario ('General' o 'Regular').</summary>
    public string Rol { get; set; } = null!;

    /// <summary>Token JWT para autenticación en requests posteriores.</summary>
    public string Token { get; set; } = null!;

    /// <summary>Tiempo de expiración del token en segundos.</summary>
    public int ExpiresIn { get; set; } = 3600; // 1 hora por defecto
}
