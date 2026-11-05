namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Venta de consumo en mesa (HU-CON-002) o para llevar (HU-CON-003).
/// Una venta de consumo es la cuenta de la mesa: nace Abierta con el primer pedido, suma pedidos
/// mientras la mesa esta ocupada y pasa a Cobrada al cobrar. La venta para llevar nace Cobrada.
/// Solo las ventas Cobradas son ingreso. IdReserva es obligatorio solo para TipoVenta = Consumo y
/// hay una sola venta por reserva. Las reglas de estado y metodo de pago las garantizan tambien
/// las restricciones CHECK de la base de datos.
/// </summary>
public class Venta
{
    public int IdVenta { get; set; }

    /// <summary>Consumo / Takeout (ver <see cref="VentaTipos"/>).</summary>
    public string TipoVenta { get; set; } = null!;

    /// <summary>Abierta / Cobrada / Anulada / Abandonada (ver <see cref="VentaEstados"/>).</summary>
    public string Estado { get; set; } = VentaEstados.Abierta;
    public int? IdReserva { get; set; }

    /// <summary>Obligatorio al cobrar con Total mayor que cero; nulo mientras la cuenta esta abierta.</summary>
    public int? IdMetodoPago { get; set; }

    /// <summary>Suma de las lineas activas; la recalcula el servicio, nunca se incrementa.</summary>
    public decimal Total { get; set; }

    /// <summary>Quien abrio la cuenta o registro la venta.</summary>
    public int IdUsuarioRegistro { get; set; }

    /// <summary>Apertura de la cuenta (el cobro esta en <see cref="FechaCobro"/>).</summary>
    public DateTime FechaVenta { get; set; }

    public DateTime? FechaCobro { get; set; }
    public int? IdUsuarioCobro { get; set; }

    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? IdUsuarioAnulacion { get; set; }

    /// <summary>Token de concurrencia: cobrar con una cuenta que cambio falla en vez de cobrar un total desactualizado.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public Reserva? Reserva { get; set; }
    public MetodoPago? MetodoPago { get; set; }
    public Usuario UsuarioRegistro { get; set; } = null!;
    public Usuario? UsuarioCobro { get; set; }
    public Usuario? UsuarioAnulacion { get; set; }
    public ICollection<PedidoVenta> Pedidos { get; set; } = new List<PedidoVenta>();
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}

public static class VentaTipos
{
    public const string Consumo = "Consumo";
    public const string Takeout = "Takeout";
}

public static class VentaEstados
{
    public const string Abierta = "Abierta";
    public const string Cobrada = "Cobrada";
    public const string Anulada = "Anulada";

    /// <summary>Cuenta con productos cerrada sin cobrar (el cliente se fue sin pagar). No es ingreso.</summary>
    public const string Abandonada = "Abandonada";
}
