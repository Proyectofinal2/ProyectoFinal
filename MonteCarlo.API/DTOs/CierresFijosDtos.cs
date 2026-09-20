using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>HU-CFG-002: días de la semana cerrados de forma recurrente (0=Domingo ... 6=Sábado).</summary>
public class CierresFijosResponse
{
    public List<int> Dias { get; set; } = [];
}

public class ActualizarCierresFijosRequest
{
    /// <summary>Selección completa: los días ausentes se dejan de aplicar como cierre.</summary>
    [Required]
    public List<int> Dias { get; set; } = [];
}
