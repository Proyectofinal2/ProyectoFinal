namespace MonteCarlo.API.Data.Entities;

/// <summary>Relacion N*N Reserva-Mesa: unica fuente de verdad de la asignacion de mesa(s).</summary>
public class ReservaMesa
{
    public int IdReserva { get; set; }
    public int IdMesa { get; set; }

    public Reserva Reserva { get; set; } = null!;
    public Mesa Mesa { get; set; } = null!;
}
