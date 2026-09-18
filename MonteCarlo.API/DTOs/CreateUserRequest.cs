using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>HU-AUT-005: solicitud para crear una nueva cuenta de administrador.</summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string NombreCompleto { get; set; } = null!;

    [Required(ErrorMessage = "El correo corporativo es requerido.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [StringLength(150)]
    public string CorreoElectronico { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [StringLength(150)]
    public string? CorreoPersonal { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Solo letras, números, punto, guion y guion bajo.")]
    public string NombreUsuario { get; set; } = null!;

    // Misma política que MonteCarlo.WEB (PoliticaContrasena): mínimo 8 caracteres,
    // con mayúscula, minúscula y número. Mantener sincronizada con ese archivo.
    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Debe incluir al menos una minúscula, una mayúscula y un número.")]
    public string ContrasenaInicial { get; set; } = null!;

    [Required(ErrorMessage = "El rol es requerido.")]
    [RegularExpression(@"^(General|Regular)$", ErrorMessage = "Rol inválido.")]
    public string Rol { get; set; } = "Regular";
}
