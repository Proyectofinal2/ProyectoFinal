namespace MonteCarlo.API.DTOs;

/// <summary>Datos de una reserva que se muestran al cliente que consulta con su código.</summary>
public class ReservaResponse
{
    public string CodigoReserva { get; set; } = null!;
    public string NombreCliente { get; set; } = null!;
    public DateOnly FechaReserva { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = null!;
    public int? NumeroMesa { get; set; }

    /// <summary>Solo Pendiente y Confirmada pueden cancelarse (HU-RES-005).</summary>
    public bool PuedeCancelarse { get; set; }
}
