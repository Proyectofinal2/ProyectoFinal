using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>Ayudas de presentacion de las cuentas.</summary>
public static class CuentaUi
{
    /// <summary>Hasta dos iniciales (primera y ultima palabra) para el avatar: «Ana Lopez Mora» -> «AM».</summary>
    public static string Iniciales(string? nombre)
    {
        var partes = (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => (partes[0][..1] + partes[^1][..1]).ToUpperInvariant()
        };
    }
}

/// <summary>Roles del modulo administrativo.</summary>
public static class Roles
{
    /// <summary>
    /// Unico rol que puede crear, desactivar y reactivar cuentas
    /// (HU-AUT-005 y HU-AUT-006).
    /// </summary>
    public const string General = "General";

    /// <summary>Sin acceso a la gestion de cuentas.</summary>
    public const string Regular = "Regular";
}

/// <summary>HU-AUT-005: alta de una cuenta de administrador.</summary>
public class NuevaCuentaViewModel
{
    [Required(ErrorMessage = "Ingresa el nombre completo.")]
    [StringLength(120)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo electronico corporativo.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [StringLength(120)]
    [Display(Name = "Correo electronico corporativo")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [StringLength(120)]
    [Display(Name = "Correo personal (opcional - para recuperacion de contrasena)")]
    public string? CorreoPersonal { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre de usuario.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Debe tener entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Solo se permiten letras, numeros, punto, guion y guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contrasena inicial.")]
    [StringLength(100, MinimumLength = PoliticaContrasena.LongitudMinima,
        ErrorMessage = PoliticaContrasena.MensajeLongitud)]
    [RegularExpression(PoliticaContrasena.Patron, ErrorMessage = PoliticaContrasena.MensajePatron)]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena inicial")]
    public string ContrasenaInicial { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = Roles.Regular;
}

/// <summary>
/// HU-AUT-007: formulario del panel lateral para editar una cuenta (nombre, usuario, correos y rol).
/// No incluye la contrasena ni el estado.
/// </summary>
public class EditarCuentaViewModel
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre completo.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo electronico corporativo.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [StringLength(120, ErrorMessage = "El correo no puede superar los 120 caracteres.")]
    [Display(Name = "Correo electronico corporativo")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [StringLength(120, ErrorMessage = "El correo no puede superar los 120 caracteres.")]
    [Display(Name = "Correo personal (opcional)")]
    public string? CorreoPersonal { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre de usuario.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Debe tener entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Solo se permiten letras, numeros, punto, guion y guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression("^(General|Regular)$", ErrorMessage = "Rol invalido.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = Roles.Regular;

    /// <summary>
    /// Cuenta tal como esta guardada (no lo enviado en el formulario). Sirve para el encabezado del modal y para
    /// que «Guardar» solo se habilite cuando algo difiere de lo guardado, tambien tras un error de validacion.
    /// </summary>
    [BindNever]
    public UserApiResponse? Original { get; set; }

    public string NombreMostrado => Original?.NombreCompleto ?? NombreCompleto;
    public string UsuarioMostrado => Original?.NombreUsuario ?? NombreUsuario;

    /// <summary>Cuenta de la sesion actual: no puede cambiar su propio rol.</summary>
    public bool EsCuentaPropia { get; set; }

    /// <summary>Unica cuenta General activa: no se le puede quitar el rol (la API lo valida tambien).</summary>
    public bool EsUltimoGeneralActivo { get; set; }

    /// <summary>Texto que explica por que el rol esta bloqueado; null si se puede cambiar.</summary>
    public string? MotivoRolBloqueado =>
        EsCuentaPropia ? "No puedes cambiar el rol de la cuenta con la que iniciaste sesion."
        : EsUltimoGeneralActivo ? "Es la unica cuenta General activa: el sistema no puede quedarse sin una."
        : null;

    public bool RolBloqueado => MotivoRolBloqueado is not null;
}

/// <summary>Una fila del listado de cuentas (HU-AUT-006, escenario 1).</summary>
public class CuentaFilaViewModel
{
    public int IdUsuario { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string NombreUsuario { get; init; } = string.Empty;
    public string CorreoElectronico { get; init; } = string.Empty;
    public string Rol { get; init; } = Roles.Regular;
    public bool Activa { get; init; }

    /// <summary>
    /// Marca la cuenta con la que se inicio la sesion actual.
    /// La vista la senala y le deshabilita la accion de desactivar
    /// (HU-AUT-006, escenario 4).
    /// </summary>
    public bool EsCuentaPropia { get; init; }

    public bool EsAdministradorGeneral => Rol == Roles.General;

    public string RolLegible => EsAdministradorGeneral
        ? "Administrador General"
        : "Administrador Regular";

    public string Iniciales => CuentaUi.Iniciales(NombreCompleto);
}

/// <summary>Filtro de estado del listado.</summary>
public enum FiltroEstadoCuenta
{
    [Display(Name = "Todas")] Todas = 0,
    [Display(Name = "Activas")] Activas = 1,
    [Display(Name = "Inactivas")] Inactivas = 2
}

/// <summary>Pantalla completa del listado de cuentas.</summary>
public class CuentasIndexViewModel
{
    [Display(Name = "Buscar")]
    public string? Busqueda { get; set; }

    [Display(Name = "Estado")]
    public FiltroEstadoCuenta Estado { get; set; } = FiltroEstadoCuenta.Todas;

    public IReadOnlyList<CuentaFilaViewModel> Cuentas { get; init; } = [];

    /// <summary>Conteos de la busqueda actual (sin importar el filtro de estado), para el control segmentado.</summary>
    public int TotalCuentas { get; init; }
    public int TotalActivas { get; init; }
    public int TotalInactivas { get; init; }

    /// <summary>Formulario de edicion que se vuelve a pintar (panel abierto) cuando el POST fallo por validacion.</summary>
    public EditarCuentaViewModel? Formulario { get; set; }

    /// <summary>
    /// True cuando hay filtros aplicados. Permite distinguir el
    /// estado vacio "aun no hay cuentas" del estado vacio "tu
    /// busqueda no arrojo resultados", que piden acciones distintas.
    /// </summary>
    public bool TieneFiltrosAplicados =>
        !string.IsNullOrWhiteSpace(Busqueda) || Estado != FiltroEstadoCuenta.Todas;
}
