using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>
/// Solicitud de inicio de sesión para administradores.
/// Requisitos: HU-AUT-001
/// </summary>
public class LoginRequest
{
    /// <summary>Nombre de usuario o correo electrónico.</summary>
    [Required(ErrorMessage = "El usuario es requerido.")]
    [StringLength(120, MinimumLength = 3,
        ErrorMessage = "El usuario debe tener entre 3 y 120 caracteres.")]
    public string Usuario { get; set; } = null!;

    /// <summary>Contraseña en texto plano (se verifica contra el hash en BD).</summary>
    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(255, MinimumLength = 6,
        ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Contrasena { get; set; } = null!;
}
