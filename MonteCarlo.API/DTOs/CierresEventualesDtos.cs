using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// Fecha puntual en la que el restaurante permanece cerrado.
public class CierreEventualResponse
{
    public int IdCierreEventual { get; set; }
    public DateOnly Fecha { get; set; }
    public string Motivo { get; set; } = null!;
    public string UsuarioCreacion { get; set; } = null!;
}

public class RegistrarCierreEventualRequest
{
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateOnly? Fecha { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(300, ErrorMessage = "El motivo no puede superar los 300 caracteres.")]
    public string? Motivo { get; set; }

    /// Confirma la cancelacion de las reservas que existan en esa fecha.
    public bool CancelarReservas { get; set; }
}
