using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>HU-AUT-007: solicitud para editar una cuenta de administrador (no incluye contraseña ni estado).</summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
    public string NombreCompleto { get; set; } = null!;

    [Required(ErrorMessage = "El correo corporativo es requerido.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
    public string CorreoElectronico { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
    public string? CorreoPersonal { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Solo letras, números, punto, guion y guion bajo.")]
    public string NombreUsuario { get; set; } = null!;

    [Required(ErrorMessage = "El rol es requerido.")]
    [RegularExpression(@"^(General|Regular)\z", ErrorMessage = "Rol inválido.")]
    public string Rol { get; set; } = null!;
}

/// <summary>Cuerpo opcional al deshabilitar o habilitar una cuenta.</summary>
public class CambiarEstadoRequest
{
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
    public string? Motivo { get; set; }
}
