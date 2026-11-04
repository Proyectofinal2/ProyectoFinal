using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>
/// La cuenta de una mesa (HU-CON-002): lo consumido por la reserva, agrupado por pedido. Mientras no hay
/// ningun pedido la cuenta esta vacia (<see cref="Estado"/> = SinPedidos) y todavia no existe la venta.
/// </summary>
public class CuentaResponse
{
    public int IdReserva { get; set; }

    /// <summary>Nulo mientras la cuenta esta vacia (la venta se abre con el primer pedido).</summary>
    public int? IdVenta { get; set; }

    /// <summary>Folio legible (V-000123); el mismo registro se llama cuenta mientras esta abierta y venta una vez cobrada.</summary>
    public string? Folio { get; set; }

    /// <summary>SinPedidos / Abierta / Cobrada / Anulada.</summary>
    public string Estado { get; set; } = "SinPedidos";

    /// <summary>Estado de la reserva (Confirmada, Finalizada...), para explicar un bloqueo.</summary>
    public string EstadoReserva { get; set; } = null!;

    public string Cliente { get; set; } = null!;
    public int Personas { get; set; }
    public List<int> NumerosMesa { get; set; } = [];
    public DateTime? FechaAtencion { get; set; }

    /// <summary>Suma de las lineas activas.</summary>
    public decimal Total { get; set; }

    /// <summary>Unidades activas (no anuladas) en la cuenta.</summary>
    public int TotalProductos { get; set; }

    /// <summary>RowVersion en base64 (solo con la cuenta abierta); se devuelve al cobrar para detectar cambios.</summary>
    public string? Version { get; set; }

    public List<PedidoCuentaResponse> Pedidos { get; set; } = [];

    public bool PuedeAgregar { get; set; }
    public bool PuedeCobrar { get; set; }

    /// <summary>Solo con la cuenta vacia y el cliente ya llegado.</summary>
    public bool PuedeLiberarSinConsumo { get; set; }

    /// <summary>Por que no se puede registrar consumo (reserva no confirmada, sin mesa...). Nulo si se puede.</summary>
    public string? Bloqueo { get; set; }
}

public class PedidoCuentaResponse
{
    public int IdPedido { get; set; }

    /// <summary>1, 2, 3... segun el orden en la cuenta.</summary>
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = null!;

    /// <summary>Suma de las lineas activas del pedido.</summary>
    public decimal Subtotal { get; set; }
    public List<LineaCuentaResponse> Lineas { get; set; } = [];
}

public class LineaCuentaResponse
{
    public int IdDetalle { get; set; }
    public int IdProducto { get; set; }
    public string Producto { get; set; } = null!;
    public int Cantidad { get; set; }

    /// <summary>Precio congelado al agregar el pedido.</summary>
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    /// <summary>La linea quitada no cuenta en el total pero conserva quien y cuando.</summary>
    public bool Anulado { get; set; }
}

/// <summary>Una ronda de pedido. El cliente solo envia producto y cantidad: precio y total los decide el servidor.</summary>
public class AgregarPedidoRequest
{
    /// <summary>GUID generado por la pantalla; un reintento con la misma clave no duplica el pedido.</summary>
    [Required(ErrorMessage = "Falta la clave del pedido.")]
    public Guid ClaveIdempotencia { get; set; }

    [Required(ErrorMessage = "Agrega al menos un producto para registrar el pedido.")]
    public List<LineaPedidoRequest>? Lineas { get; set; }
}

public class LineaPedidoRequest
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
}

/// <summary>Quita una linea de una cuenta abierta. Sin cantidad (o igual a la de la linea) se quita completa.</summary>
public class AnularLineaRequest
{
    /// <summary>Unidades a quitar; si es menor que la cantidad de la linea queda una linea de reemplazo con el resto.</summary>
    public int? Cantidad { get; set; }

    [StringLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string? Motivo { get; set; }
}

public class CobrarRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Elige el método de pago.")]
    public int IdMetodoPago { get; set; }

    /// <summary>El total que vio la pantalla: si la cuenta cambio mientras tanto, no se cobra (409).</summary>
    public decimal TotalEsperado { get; set; }

    /// <summary>
    /// Version (RowVersion en base64) de la cuenta que vio la pantalla. Se exige junto al total: dos cambios que se
    /// compensan (quitar un producto y agregar otro del mismo precio) dejan el mismo total pero cambian la version.
    /// </summary>
    [Required(ErrorMessage = "Falta la version de la cuenta.")]
    public string? Version { get; set; }
}

/// <summary>Resultado de cobrar o de liberar una mesa sin consumo.</summary>
public class CobroResponse
{
    public int IdVenta { get; set; }
    public string Folio { get; set; } = null!;
    public int IdReserva { get; set; }
    public List<int> NumerosMesa { get; set; } = [];
    public decimal Total { get; set; }

    /// <summary>Nulo en un cierre sin consumo.</summary>
    public string? MetodoPago { get; set; }
}

public class MetodoPagoResponse
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = null!;
}

/// <summary>Catalogo vendible agrupado por categoria, para el selector de la caja.</summary>
public class CategoriaVentaResponse
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = null!;
    public int Orden { get; set; }
    public List<ProductoVentaResponse> Productos { get; set; } = [];
}

public class ProductoVentaResponse
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
}

/// HU-CON-004: Fila del listado de ventas.
public class VentaListadoResponse
{
    public int IdVenta { get; set; }
    public string? Folio { get; set; }
    public DateTime FechaVenta { get; set; }
    public string TipoVenta { get; set; } = null!;
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public string Estado { get; set; } = null!;
}

/// HU-CON-004: Filtros para consultar el listado de ventas.
public class FiltroVentasRequest
{
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public string? TipoVenta { get; set; }
    public int? IdMetodoPago { get; set; }
    public string? Estado { get; set; }
}

/// HU-CON-005: Detalle de una venta registrada.
public class VentaDetalleResponse
{
    public int IdVenta { get; set; }
    public string Folio { get; set; } = null!;
    public DateTime FechaVenta { get; set; }
    public string TipoVenta { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public string UsuarioRegistro { get; set; } = null!;

    public List<LineaDetalleResponse> Lineas { get; set; } = [];
}

public class LineaDetalleResponse
{
    public string Producto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
