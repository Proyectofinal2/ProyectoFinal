namespace MonteCarlo.API.Data.Entities;

public class MetodoPago
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; } = true;

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
