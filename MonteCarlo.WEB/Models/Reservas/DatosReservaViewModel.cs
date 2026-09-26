using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Reservas;

/// <summary>
/// Datos que captura el paso 2 del flujo de reserva (HU-RES-003).
/// Recibe por hidden fields la fecha, hora y cantidad de personas
/// seleccionadas en el paso 1 (HU-RES-002).
/// </summary>
public class DatosReservaViewModel
{
    // ---------- Datos del paso 1 (vienen ocultos) ----------

    [Required]
    public DateOnly FechaReserva { get; set; }

    [Required]
    public TimeOnly HoraReserva { get; set; }

    [Required]
    [Range(1, 50)]
    public int CantidadPersonas { get; set; }

    // ---------- Datos del paso 2 (los captura el cliente) ----------

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Apellido")]
    public string? Apellido { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(120)]
    [Display(Name = "Correo electrónico")]
    public string? CorreoElectronico { get; set; }

    [StringLength(300)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}