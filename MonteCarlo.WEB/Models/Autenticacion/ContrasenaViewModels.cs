using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>
/// Politica de contrasena, en un solo lugar para que las tres
/// pantallas que piden una contrasena nueva (HU-AUT-003, HU-AUT-004
/// y HU-AUT-005) no se contradigan entre si.
/// </summary>
public static class PoliticaContrasena
{
    public const int LongitudMinima = 8;

    /// <summary>Al menos una minuscula, una mayuscula y un digito.</summary>
    public const string Patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";

    public const string MensajePatron =
        "Debe incluir al menos una minuscula, una mayuscula y un numero.";

    public const string MensajeLongitud =
        "Debe tener al menos 8 caracteres.";

    /// <summary>Texto de ayuda que se muestra bajo el campo.</summary>
    public const string Ayuda =
        "Minimo 8 caracteres, con mayusculas, minusculas y al menos un numero.";
}

/// <summary>HU-AUT-003, escenario 1: solicitud del enlace.</summary>
public class RecuperarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo electronico.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [Display(Name = "Correo electronico")]
    public string CorreoElectronico { get; set; } = string.Empty;
}

/// <summary>HU-AUT-003, escenario 2: nueva contrasena via enlace.</summary>
public class RestablecerContrasenaViewModel
{
    /// <summary>Token del enlace. Viaja oculto en el formulario.</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la contrasena nueva.")]
    [StringLength(100, MinimumLength = PoliticaContrasena.LongitudMinima,
        ErrorMessage = PoliticaContrasena.MensajeLongitud)]
    [RegularExpression(PoliticaContrasena.Patron, ErrorMessage = PoliticaContrasena.MensajePatron)]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena nueva")]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contrasena nueva.")]
    [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contrasenas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contrasena")]
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}

/// <summary>HU-AUT-004: cambio desde Mi Cuenta, con sesion activa.</summary>
public class CambiarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingresa tu contrasena actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena actual")]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la contrasena nueva.")]
    [StringLength(100, MinimumLength = PoliticaContrasena.LongitudMinima,
        ErrorMessage = PoliticaContrasena.MensajeLongitud)]
    [RegularExpression(PoliticaContrasena.Patron, ErrorMessage = PoliticaContrasena.MensajePatron)]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena nueva")]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contrasena nueva.")]
    [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contrasenas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contrasena nueva")]
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}
