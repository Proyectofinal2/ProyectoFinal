namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Catalogo de productos vendibles (consumo en mesa o takeout) - HU-CON-001.
/// </summary>
public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Categoria { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
}
