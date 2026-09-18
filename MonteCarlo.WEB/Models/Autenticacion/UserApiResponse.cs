namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>Respuesta del API después de crear o actualizar un usuario.</summary>
public class UserApiResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string NombreUsuario { get; set; } = null!;
    public string CorreoElectronico { get; set; } = null!;
    public string? CorreoPersonal { get; set; }
    public string Rol { get; set; } = null!;
    public bool Activo { get; set; }
}
