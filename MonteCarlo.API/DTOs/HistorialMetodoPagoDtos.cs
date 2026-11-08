namespace MonteCarlo.API.DTOs;

public class HistorialMetodoPagoResponse
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
    public string Usuario { get; set; } = null!;
    public string RolUsuario { get; set; } = null!;
    public DateTime Fecha { get; set; }
}
