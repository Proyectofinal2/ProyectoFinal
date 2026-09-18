using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Autenticacion;

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
    [StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo electronico corporativo.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [StringLength(150)]
    [Display(Name = "Correo electronico corporativo")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [StringLength(150)]
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

    /// <summary>
    /// True cuando hay filtros aplicados. Permite distinguir el
    /// estado vacio "aun no hay cuentas" del estado vacio "tu
    /// busqueda no arrojo resultados", que piden acciones distintas.
    /// </summary>
    public bool TieneFiltrosAplicados =>
        !string.IsNullOrWhiteSpace(Busqueda) || Estado != FiltroEstadoCuenta.Todas;
}
