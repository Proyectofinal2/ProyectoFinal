using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>Solicitud para cambiar la contraseña de la cuenta autenticada. HU-AUT-004.</summary>
public class ChangePasswordRequest
{
    /// <summary>Contraseña vigente de la cuenta autenticada.</summary>
    [Required(ErrorMessage = "La contraseña actual es requerida.")]
    public string ContrasenaActual { get; set; } = null!;

    /// <summary>Nueva contraseña en texto plano; se hashea antes de persistir.</summary>
    [Required(ErrorMessage = "La nueva contraseña es requerida.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "La nueva contraseña debe tener entre 8 y 100 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La nueva contraseña debe incluir al menos una minúscula, una mayúscula y un número.")]
    public string NuevaContrasena { get; set; } = null!;
}
