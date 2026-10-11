namespace MonteCarlo.API.Data.Entities;

public class Mesa
{
    public int IdMesa { get; set; }
    public int NumeroMesa { get; set; }
    public int Capacidad { get; set; }
    public string Estado { get; set; } = "Disponible";

    /// <summary>Posicion en el canvas del editor visual de mesas (admin/mesas).</summary>
    public int PosX { get; set; }
    public int PosY { get; set; }

    public ICollection<ReservaMesa> ReservaMesas { get; set; } = new List<ReservaMesa>();
}
