using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>
/// Solicitud de recuperación de contraseña.
/// Requisitos: HU-AUT-003
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>Nombre de usuario o correo electrónico.</summary>
    [Required(ErrorMessage = "El usuario es requerido.")]
    [StringLength(120, MinimumLength = 3,
        ErrorMessage = "El usuario debe tener entre 3 y 120 caracteres.")]
    public string UsuarioOCorreo { get; set; } = null!;
}

/// <summary>
/// Solicitud de restablecimiento de contraseña con el token recibido por correo.
/// Requisitos: HU-AUT-003
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>Token de recuperación recibido por correo.</summary>
    [Required(ErrorMessage = "El token es requerido.")]
    public string Token { get; set; } = null!;

    /// <summary>Nueva contraseña en texto plano (se hashea antes de persistir).</summary>
    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(255, MinimumLength = 6,
        ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string NuevaContrasena { get; set; } = null!;
}
