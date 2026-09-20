using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Configuracion;

/// Respuesta de GET api/configuracion/umbral-confirmacion.
public class UmbralApiResponse
{
    public int Umbral { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}

///HU-CFG-001: formulario del umbral de confirmación automática.
public class UmbralViewModel
{
    [Display(Name = "Umbral de confirmación automática (personas)")]
    [Required(ErrorMessage = "Ingresa un valor para el umbral.")]
    [Range(1, 1000, ErrorMessage = "El umbral debe ser un número entero positivo.")]
    public int? Umbral { get; set; }

    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}

/// Respuesta de GET api/cierres-fijos (0=Domingo ... 6=Sábado).
public class CierresFijosApiResponse
{
    public List<int> Dias { get; set; } = [];
}

/// HU-CFG-002: días de la semana seleccionados como cierre fijo.
public class CierresFijosViewModel
{
    public List<int> Dias { get; set; } = [];

    /// Orden de presentación: la semana empieza el lunes (valor 0 = domingo).
    public static readonly IReadOnlyList<(int Valor, string Nombre)> DiasSemana =
    [
        (1, "Lunes"), (2, "Martes"), (3, "Miércoles"), (4, "Jueves"),
        (5, "Viernes"), (6, "Sábado"), (0, "Domingo")
    ];
}
