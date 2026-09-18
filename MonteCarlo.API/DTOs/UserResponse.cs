namespace MonteCarlo.API.DTOs;

/// <summary>Información pública de un usuario después de una operación (crear, actualizar, etc).</summary>
public class UserResponse
{
    /// <summary>Identificador del usuario.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Nombre completo del usuario.</summary>
    public string NombreCompleto { get; set; } = null!;

    /// <summary>Nombre de usuario (login).</summary>
    public string NombreUsuario { get; set; } = null!;

    /// <summary>Correo electrónico institucional del usuario.</summary>
    public string CorreoElectronico { get; set; } = null!;

    /// <summary>Correo electrónico personal del usuario (opcional).</summary>
    public string? CorreoPersonal { get; set; }

    /// <summary>Rol asignado al usuario.</summary>
    public string Rol { get; set; } = null!;

    /// <summary>Indica si el usuario está activo.</summary>
    public bool Activo { get; set; }
}
