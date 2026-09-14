namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Venta de consumo en mesa (HU-CON-002) o takeout (HU-CON-003).
/// IdReserva es obligatorio solo para TipoVenta = Consumo.
/// IdMetodoPago puede quedar nulo unicamente si Total = 0; esa regla se valida
/// en la capa de aplicacion.
/// </summary>
public class Venta
{
    public int IdVenta { get; set; }

    /// <summary>Consumo / Takeout.</summary>
    public string TipoVenta { get; set; } = null!;
    public int? IdReserva { get; set; }
    public int? IdMetodoPago { get; set; }
    public decimal Total { get; set; }
    public int IdUsuarioRegistro { get; set; }
    public DateTime FechaVenta { get; set; }

    public Reserva? Reserva { get; set; }
    public MetodoPago? MetodoPago { get; set; }
    public Usuario UsuarioRegistro { get; set; } = null!;
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}
