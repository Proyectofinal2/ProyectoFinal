namespace MonteCarlo.API.Data.Entities;

/// <summary>Auditoría de cada creación o cambio efectivo de un método de pago.</summary>
public class HistorialMetodoPago
{
    public int IdHistorialMetodoPago { get; set; }
    public Guid IdOperacion { get; set; }
    public int IdMetodoPago { get; set; }
    public string Accion { get; set; } = null!;
    public string? NombreAnterior { get; set; }
    public string NombreNuevo { get; set; } = null!;
    public bool? ActivoAnterior { get; set; }
    public bool ActivoNuevo { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; }

    public MetodoPago MetodoPago { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public static class HistorialMetodoPagoAcciones
{
    public const string Creado = "Creado";
    public const string Modificado = "Modificado";

    public static readonly string[] Todas = [Creado, Modificado];
}
