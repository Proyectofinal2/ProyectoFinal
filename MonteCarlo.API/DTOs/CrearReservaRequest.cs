using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>
/// Datos que envía el cliente desde el formulario de la reserva (HU-RES-003).
/// </summary>
public class CrearReservaRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = null!;

    [StringLength(80, ErrorMessage = "El apellido no puede superar los 80 caracteres.")]
    public string? Apellido { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string Telefono { get; set; } = null!;

    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(120, ErrorMessage = "El correo no puede superar los 120 caracteres.")]
    public string? CorreoElectronico { get; set; }

    [Required(ErrorMessage = "La fecha de la reserva es obligatoria.")]
    public DateOnly FechaReserva { get; set; }

    [Required(ErrorMessage = "La hora de la reserva es obligatoria.")]
    public TimeOnly HoraReserva { get; set; }

    [Range(1, 50, ErrorMessage = "La cantidad de personas debe estar entre 1 y 50.")]
    public int CantidadPersonas { get; set; }

    [StringLength(300, ErrorMessage = "Las observaciones no pueden superar los 300 caracteres.")]
    public string? Observaciones { get; set; }
}