namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Auditoria de cambios en cuentas de administrador (HU-AUT-007). Una fila por campo cambiado
/// en una edicion; una fila por alta, deshabilitacion o habilitacion. Nunca se guardan
/// contrasenas ni hashes.
/// </summary>
public class HistorialUsuario
{
    public int IdHistorialUsuario { get; set; }

    /// <summary>Cuenta que cambio.</summary>
    public int IdUsuarioAfectado { get; set; }

    /// <summary>Nombre completo de la cuenta afectada al momento del cambio.</summary>
    public string NombreUsuarioAfectado { get; set; } = null!;

    /// <summary>Creada / Editada / Deshabilitada / Habilitada (ver <see cref="HistorialUsuarioAcciones"/>).</summary>
    public string Accion { get; set; } = null!;

    /// <summary>Obligatorio en Editada (ver <see cref="HistorialUsuarioCampos"/>).</summary>
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }

    /// <summary>Motivo opcional que indica el administrador al deshabilitar o habilitar.</summary>
    public string? Motivo { get; set; }

    /// <summary>Administrador responsable del cambio.</summary>
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; }

    public Usuario UsuarioAfectado { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public static class HistorialUsuarioAcciones
{
    public const string Creada = "Creada";
    public const string Editada = "Editada";
    public const string Deshabilitada = "Deshabilitada";
    public const string Habilitada = "Habilitada";

    public static readonly string[] Todas = [Creada, Editada, Deshabilitada, Habilitada];
}

public static class HistorialUsuarioCampos
{
    public const string NombreCompleto = "NombreCompleto";
    public const string NombreUsuario = "NombreUsuario";
    public const string CorreoElectronico = "CorreoElectronico";
    public const string CorreoPersonal = "CorreoPersonal";
    public const string Rol = "Rol";
    public const string Activo = "Activo";
}
