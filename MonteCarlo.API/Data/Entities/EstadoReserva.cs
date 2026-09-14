namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Catalogo de estados de una reserva: Pendiente, Confirmada, Cancelada, Finalizada, No Show.
/// </summary>
public class EstadoReserva
{
    public int IdEstadoReserva { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
