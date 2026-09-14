namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Datos capturados al reservar (sin cuenta / sin login).
/// </summary>
public class Cliente
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Apellido { get; set; }
    public string Telefono { get; set; } = null!;
    public string? CorreoElectronico { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
