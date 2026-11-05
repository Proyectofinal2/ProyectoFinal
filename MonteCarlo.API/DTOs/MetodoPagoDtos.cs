using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

public class MetodoPagoAdminResponse
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }
}

public class CrearMetodoPagoRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(30, ErrorMessage = "El nombre no puede superar los 30 caracteres.")]
    public string Nombre { get; set; } = null!;
}

public class CambiarEstadoMetodoPagoRequest
{
    public bool Activo { get; set; }
}