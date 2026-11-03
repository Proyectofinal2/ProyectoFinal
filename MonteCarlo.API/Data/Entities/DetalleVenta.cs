namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Linea de una cuenta. Pertenece a un pedido de su misma venta (FK compuesta). Quitarla de una
/// cuenta abierta la marca <see cref="Anulado"/> con quien y cuando, sin borrarla. Las lineas no se
/// unen por producto: cada pedido conserva las suyas.
/// </summary>
public class DetalleVenta
{
    public int IdDetalleVenta { get; set; }
    public int IdVenta { get; set; }
    public int IdPedido { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }

    /// <summary>Precio historico (snapshot) congelado al agregar el pedido, independiente de Producto.Precio.</summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>Columna calculada y persistida en la base de datos (Cantidad * PrecioUnitario).</summary>
    public decimal Subtotal { get; private set; }

    public bool Anulado { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? IdUsuarioAnulacion { get; set; }

    public Venta Venta { get; set; } = null!;
    public PedidoVenta Pedido { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
    public Usuario? UsuarioAnulacion { get; set; }
}
