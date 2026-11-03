namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Una ronda de pedido dentro de una cuenta (HU-CON-002): quien la registro, cuando y una clave
/// de idempotencia para que un doble clic o un reintento no duplique las lineas.
/// </summary>
public class PedidoVenta
{
    public int IdPedido { get; set; }
    public int IdVenta { get; set; }

    /// <summary>Administrador que registro el pedido.</summary>
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; }

    /// <summary>GUID generado por la pantalla; unico cuando existe.</summary>
    public Guid? ClaveIdempotencia { get; set; }
    public string? Nota { get; set; }

    public Venta Venta { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}
