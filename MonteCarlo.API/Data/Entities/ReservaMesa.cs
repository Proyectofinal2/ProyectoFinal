namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Relacion N*N Reserva-Mesa. Una reserva usa 1 mesa (caso normal) o 2 mesas
/// adyacentes combinadas. Unica fuente de verdad de la asignacion de mesa(s).
/// </summary>
public class ReservaMesa
{
    public int IdReserva { get; set; }
    public int IdMesa { get; set; }

    public Reserva Reserva { get; set; } = null!;
    public Mesa Mesa { get; set; } = null!;
}
