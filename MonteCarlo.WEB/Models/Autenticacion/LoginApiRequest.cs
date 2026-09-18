namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>
/// Solicitud de login que se envía a la API.
/// </summary>
public class LoginApiRequest
{
    /// <summary>Nombre de usuario o correo electrónico.</summary>
    public string Usuario { get; set; } = null!;

    /// <summary>Contraseña en texto plano.</summary>
    public string Contrasena { get; set; } = null!;
}
