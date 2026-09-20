using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>HU-CFG-001: valor vigente del umbral y quién/cuándo lo cambió por última vez.</summary>
public class UmbralConfirmacionResponse
{
    public int Umbral { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}

public class ActualizarUmbralRequest
{
    [Required(ErrorMessage = "El umbral es obligatorio.")]
    [Range(1, 1000, ErrorMessage = "El umbral debe ser un número entero positivo.")]
    public int? Umbral { get; set; }
}
