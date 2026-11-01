using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Configuracion;

/// Un parámetro tal como lo devuelve la API, dentro de GET/PUT api/configuracion.
public class ParametroConfiguracionApiResponse
{
    public string Clave { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Valor { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}

/// Respuesta de GET/PUT api/configuracion (pantalla dinámica de Parametros): la
/// lista completa de lo que exista en ConfiguracionSistema, no un set fijo de claves.
public class ConfiguracionGeneralApiResponse
{
    public List<ParametroConfiguracionApiResponse> Parametros { get; set; } = [];
}

/// HU-CFG-001 / HU-CFG-006: un parámetro del sistema en el formulario de Parametros.
/// Trae su "ValorOriginal" (oculto en la vista) para poder detectar cambios y
/// enviar a la API solo lo que realmente cambió.
public class ParametroViewModel
{
    public string Clave { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Este valor es obligatorio.")]
    public string Valor { get; set; } = string.Empty;

    public string ValorOriginal { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}

/// Un único formulario con un único botón "Guardar cambios", que se construye
/// dinámicamente a partir de lo que exista en ConfiguracionSistema.
public class ParametrosViewModel
{
    public List<ParametroViewModel> Parametros { get; set; } = [];

    /// <summary>Compara cada valor contra su "ValorOriginal" y devuelve solo los que
    /// cambiaron, listos para enviar a la API como PATCH simplificado.</summary>
    public List<(string Clave, string Valor)> ObtenerCambios() =>
        Parametros
            .Where(p => p.Valor != p.ValorOriginal)
            .Select(p => (p.Clave, p.Valor))
            .ToList();
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

    /// Cierres eventuales proximos.
    public List<CierreEventualApiResponse> Eventuales { get; set; } = [];

    /// Orden de presentación: la semana empieza el lunes (valor 0 = domingo).
    public static readonly IReadOnlyList<(int Valor, string Nombre)> DiasSemana =
    [
        (1, "Lunes"), (2, "Martes"), (3, "Miércoles"), (4, "Jueves"),
        (5, "Viernes"), (6, "Sábado"), (0, "Domingo")
    ];
}

/// Respuesta de GET api/cierres-eventuales.
public class CierreEventualApiResponse
{
    public int IdCierreEventual { get; set; }
    public DateOnly Fecha { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string UsuarioCreacion { get; set; } = string.Empty;
}

/// Formulario para cerrar una fecha puntual.
public class NuevoCierreEventualViewModel
{
    [Required(ErrorMessage = "Selecciona la fecha que se va a cerrar.")]
    public DateOnly? Fecha { get; set; }

    [Required(ErrorMessage = "Indica el motivo del cierre.")]
    [StringLength(300, ErrorMessage = "El motivo no puede superar los 300 caracteres.")]
    public string? Motivo { get; set; }

    /// Confirma la cancelacion de las reservas que existan en esa fecha.
    public bool CancelarReservas { get; set; }
}

/// Fila de GET api/historial-cierres.
public class HistorialCierreApiResponse
{
    public DateTime Fecha { get; set; }
    public string TipoCierre { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public int? DiaSemana { get; set; }
    public DateOnly? FechaCierre { get; set; }
    public string? Motivo { get; set; }
    public string Usuario { get; set; } = string.Empty;
}

/// Historial de cierres con sus filtros.
public class HistorialCierresViewModel
{
    public string? Tipo { get; set; }
    public string? Accion { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public List<HistorialCierreApiResponse> Items { get; set; } = [];

    public bool TieneFiltrosAplicados =>
        !string.IsNullOrEmpty(Tipo) || !string.IsNullOrEmpty(Accion) || Desde.HasValue || Hasta.HasValue;
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
public class HorarioOperacionDiaViewModel : IValidatableObject
{
    public int DiaSemana { get; set; }
    public string NombreDia { get; set; } = string.Empty;
    public TimeOnly? HoraApertura { get; set; }
    public TimeOnly? HoraCierre { get; set; }
    public bool EsCierreFijo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EsCierreFijo)
        {
            yield break;
        }

        // Un dia sin ambas horas permanece sin configurar. Si se indica una de
        // ellas, la otra pasa a ser obligatoria para evitar rangos incompletos.
        if (!HoraApertura.HasValue && !HoraCierre.HasValue)
        {
            yield break;
        }

        if (!HoraApertura.HasValue)
        {
            yield return new ValidationResult("La hora de apertura es obligatoria.", [nameof(HoraApertura)]);
        }

        if (!HoraCierre.HasValue)
        {
            yield return new ValidationResult("La hora de cierre es obligatoria.", [nameof(HoraCierre)]);
        }

        if (HoraApertura.HasValue && HoraCierre.HasValue && HoraCierre <= HoraApertura)
        {
            yield return new ValidationResult("La hora de cierre debe ser posterior a la hora de apertura.", [nameof(HoraCierre)]);
        }
    }
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
