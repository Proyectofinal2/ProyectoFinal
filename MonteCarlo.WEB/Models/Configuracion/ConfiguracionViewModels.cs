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

/// Respuesta de GET/PUT api/horarios-operacion.
public class HorarioOperacionApiResponse
{
    public int DiaSemana { get; set; }
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }
    public bool Activo { get; set; }
}

/// HU-CFG-005: datos de un día mostrados en la configuración de horarios.
public class HorarioOperacionDiaViewModel
{
    public int DiaSemana { get; set; }
    public string NombreDia { get; set; } = string.Empty;
    public TimeOnly? HoraApertura { get; set; }
    public TimeOnly? HoraCierre { get; set; }
    public bool EsCierreFijo { get; set; }
}

public class HorariosOperacionViewModel
{
    public List<HorarioOperacionDiaViewModel> Dias { get; set; } = [];
    public static readonly IReadOnlyList<(int Valor, string Nombre)> DiasSemana = CierresFijosViewModel.DiasSemana;
}

public class GuardarHorarioOperacionViewModel : IValidatableObject
{
    [Range(0, 6, ErrorMessage = "El día debe estar entre 0 (domingo) y 6 (sábado).")]
    public int DiaSemana { get; set; }

    [Required(ErrorMessage = "La hora de apertura es obligatoria.")]
    public TimeOnly? HoraApertura { get; set; }

    [Required(ErrorMessage = "La hora de cierre es obligatoria.")]
    public TimeOnly? HoraCierre { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HoraApertura.HasValue && HoraCierre.HasValue && HoraCierre <= HoraApertura)
            yield return new ValidationResult("La hora de cierre debe ser posterior a la hora de apertura.", [nameof(HoraCierre)]);
    }
}
