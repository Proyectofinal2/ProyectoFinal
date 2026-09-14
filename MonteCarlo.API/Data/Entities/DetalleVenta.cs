namespace MonteCarlo.API.Data.Entities;

public class DetalleVenta
{
    public int IdDetalleVenta { get; set; }
    public int IdVenta { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }

    /// <summary>Precio historico (snapshot), independiente de Producto.Precio.</summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>Columna calculada y persistida en la base de datos (Cantidad * PrecioUnitario).</summary>
    public decimal Subtotal { get; private set; }

    public Venta Venta { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}
