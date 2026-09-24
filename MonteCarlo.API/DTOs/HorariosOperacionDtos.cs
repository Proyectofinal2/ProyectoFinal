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
