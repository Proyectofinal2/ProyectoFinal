namespace MonteCarlo.API.DTOs;

/// <summary>Una fila del historial de cuentas de administrador (HU-AUT-007).</summary>
public class HistorialUsuarioResponse
{
    public int IdHistorialUsuario { get; set; }
    public DateTime Fecha { get; set; }

    /// <summary>Administrador que hizo el cambio.</summary>
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = null!;

    /// <summary>Rol del administrador que hizo el cambio: General / Regular.</summary>
    public string RolUsuario { get; set; } = null!;

    public int IdUsuarioAfectado { get; set; }
    public string NombreUsuarioAfectado { get; set; } = null!;

    /// <summary>Creada / Editada / Deshabilitada / Habilitada.</summary>
    public string Accion { get; set; } = null!;

    /// <summary>NombreCompleto / NombreUsuario / CorreoElectronico / CorreoPersonal / Rol / Activo.</summary>
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? Motivo { get; set; }
}
