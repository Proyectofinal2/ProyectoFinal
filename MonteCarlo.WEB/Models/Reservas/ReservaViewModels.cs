using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.WEB.Models.Reservas;

///Reserva tal como la devuelve la API (GET api/reservas/{codigo}).
public class ReservaApiResponse
{
    public string CodigoReserva { get; set; } = null!;
    public string NombreCliente { get; set; } = null!;
    public DateOnly FechaReserva { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = null!;
    public int? NumeroMesa { get; set; }
    public bool PuedeCancelarse { get; set; }
}

///Pantalla pública "Ya tengo una reserva" (HU-RES-005).
public class ConsultarReservaViewModel
{
    [Display(Name = "Código de reserva")]
    [Required(ErrorMessage = "Ingresa el código de tu reserva.")]
    [StringLength(10, ErrorMessage = "El código no puede superar los 10 caracteres.")]
    public string? Codigo { get; set; }

    /// Reserva encontrada; null si aún no se buscó o el código no existe.
    public ReservaApiResponse? Reserva { get; set; }
}

public class DisponibilidadFechaApiResponse
{
    public DateOnly Fecha { get; set; }
    public bool Disponible { get; set; }
    public string? MotivoNoDisponible { get; set; }
    public List<TimeOnly> HorariosDisponibles { get; set; } = [];
}

public class DisponibilidadMesApiResponse
{
    public int CantidadPersonas { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public List<DisponibilidadFechaApiResponse> Fechas { get; set; } = [];
}