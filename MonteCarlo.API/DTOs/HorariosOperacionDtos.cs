using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>Horario regular configurado para un día de la semana (0=Domingo ... 6=Sábado).</summary>
public class HorarioOperacionResponse
{
    public int DiaSemana { get; set; }
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }
    public bool Activo { get; set; }
}

public class GuardarHorarioOperacionRequest
{
    [Required(ErrorMessage = "El día de la semana es obligatorio.")]
    [Range(0, 6, ErrorMessage = "El día debe estar entre 0 (domingo) y 6 (sábado).")]
    public int? DiaSemana { get; set; }

    [Required(ErrorMessage = "La hora de apertura es obligatoria.")]
    public TimeOnly? HoraApertura { get; set; }

    [Required(ErrorMessage = "La hora de cierre es obligatoria.")]
    public TimeOnly? HoraCierre { get; set; }
}

/// <summary>Solicitud para guardar varios horarios de operacion en una sola transaccion.</summary>
public class GuardarHorariosOperacionLoteRequest
{
    public List<GuardarHorarioOperacionLoteItemRequest>? Horarios { get; set; }
}

/// <summary>
/// Un horario dentro de un lote. La validacion se realiza en el servicio para que
/// el lote completo reciba una respuesta ApiResponse uniforme.
/// </summary>
public class GuardarHorarioOperacionLoteItemRequest
{
    public int? DiaSemana { get; set; }
    public TimeOnly? HoraApertura { get; set; }
    public TimeOnly? HoraCierre { get; set; }
}
