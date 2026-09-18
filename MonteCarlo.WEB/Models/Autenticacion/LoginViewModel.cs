using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Autenticacion;

/// <summary>HU-AUT-001: inicio de sesion del administrador.</summary>
public class LoginViewModel
{
    /// <summary>
    /// Correo o nombre de usuario. Se acepta cualquiera de los dos
    /// para no obligar al administrador a recordar con cual se
    /// registro.
    /// </summary>
    [Required(ErrorMessage = "Ingresa tu correo o nombre de usuario.")]
    [Display(Name = "Correo o nombre de usuario")]
    public string Identificador { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contrasena.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Destino al que volver tras autenticarse. Lo completa el
    /// controlador desde la query string.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Escenario 3: segundos que faltan para que expire el bloqueo
    /// temporal. Cuando trae valor, la vista muestra la cuenta
    /// regresiva y deshabilita el formulario.
    /// </summary>
    public int? SegundosBloqueoRestantes { get; set; }

    public bool EstaBloqueado => SegundosBloqueoRestantes is > 0;
}
