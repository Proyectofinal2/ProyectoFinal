namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Catalogo de productos vendibles (consumo en mesa o takeout) - HU-CON-001.
/// Precio en colones (CRC). Solo se elimina un producto sin ventas; los demas se deshabilitan para conservar el historial de ventas.
/// </summary>
public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;
    public int IdCategoria { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public CategoriaProducto Categoria { get; set; } = null!;
    public ICollection<HistorialCatalogo> Historial { get; set; } = new List<HistorialCatalogo>();
    public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
}
