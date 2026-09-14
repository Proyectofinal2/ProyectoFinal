namespace MonteCarlo.API.Data.Entities;

public class Mesa
{
    public int IdMesa { get; set; }
    public int NumeroMesa { get; set; }
    public int Capacidad { get; set; }
    public string Estado { get; set; } = "Disponible";

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
