using MonteCarlo.WEB.Models.Catalogo;

namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>Una fila del historial de cuentas de administrador (HU-AUT-007).</summary>
public class HistorialUsuarioApiResponse
{
    public int IdHistorialUsuario { get; set; }
    public DateTime Fecha { get; set; }

    /// <summary>Administrador que hizo el cambio.</summary>
    public string Usuario { get; set; } = "";

    /// <summary>General / Regular.</summary>
    public string RolUsuario { get; set; } = "";

    public string RolLegible => string.IsNullOrEmpty(RolUsuario) ? "" : $"Administrador {RolUsuario}";

    public int IdUsuarioAfectado { get; set; }
    public string NombreUsuarioAfectado { get; set; } = "";

    /// <summary>Creada / Editada / Deshabilitada / Habilitada.</summary>
    public string Accion { get; set; } = "";
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? Motivo { get; set; }

    /// <summary>Etiqueta legible del campo (la API guarda claves sin espacios ni tildes).</summary>
    public string CampoLegible => Campo switch
    {
        "NombreCompleto" => "Nombre completo",
        "NombreUsuario" => "Nombre de usuario",
        "CorreoElectronico" => "Correo corporativo",
        "CorreoPersonal" => "Correo personal",
        "Rol" => "Rol",
        "Activo" => "Estado",
        _ => Campo ?? ""
    };

    /// <summary>Valor legible: el rol se muestra completo y un valor vacio como «(sin valor)».</summary>
    public string FormatearValor(string? valor) => valor switch
    {
        null or "" => "(sin valor)",
        "General" when Campo == "Rol" => "Administrador General",
        "Regular" when Campo == "Rol" => "Administrador Regular",
        _ => valor
    };

    public TonoBadgeHistorial Tono => Accion switch
    {
        "Creada" => TonoBadgeHistorial.Informativo,
        "Habilitada" => TonoBadgeHistorial.Exito,
        "Deshabilitada" => TonoBadgeHistorial.Neutral,
        _ => TonoBadgeHistorial.Neutral
    };
}

public enum TonoBadgeHistorial
{
    Neutral,
    Exito,
    Informativo
}

/// <summary>Pantalla del historial de cuentas: filtros y una pagina de resultados.</summary>
public class HistorialUsuariosViewModel
{
    public string? Busqueda { get; set; }
    public int? IdUsuario { get; set; }
    public string? Accion { get; set; }

    public List<UsuarioHistorialApiResponse> Responsables { get; set; } = [];
    public PaginaApiResponse<HistorialUsuarioApiResponse> Pagina { get; set; } = new();

    public bool TieneFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda) || IdUsuario.HasValue || !string.IsNullOrEmpty(Accion);
}
