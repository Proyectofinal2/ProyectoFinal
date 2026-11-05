using MonteCarlo.WEB.Models.Catalogo;

namespace MonteCarlo.WEB.Models.Ventas;

// ------------------------------------------------------------
// Tal como los devuelve api/ventas, api/metodos-pago y api/productos/venta (HU-CON-002)
// ------------------------------------------------------------

public class CuentaApiResponse
{
    public int IdReserva { get; set; }
    public int? IdVenta { get; set; }
    public string? Folio { get; set; }

    /// <summary>SinPedidos / Abierta / Cobrada / Anulada.</summary>
    public string Estado { get; set; } = "SinPedidos";
    public string EstadoReserva { get; set; } = "";
    public string Cliente { get; set; } = "";
    public int Personas { get; set; }
    public List<int> NumerosMesa { get; set; } = [];
    public DateTime? FechaAtencion { get; set; }
    public decimal Total { get; set; }
    public int TotalProductos { get; set; }
    public string? Version { get; set; }
    public List<PedidoCuentaApiResponse> Pedidos { get; set; } = [];
    public bool PuedeAgregar { get; set; }
    public bool PuedeCobrar { get; set; }
    public bool PuedeLiberarSinConsumo { get; set; }
    public bool PuedeCerrarSinCobro { get; set; }
    public string? Bloqueo { get; set; }

    /// <summary>La cuenta abierta paso el tiempo de uso de la mesa (llegada + duracion del turno) mas la tolerancia para cobrar.</summary>
    public bool Antigua { get; set; }
    public DateTime? VenceDesde { get; set; }

    /// <summary>
    /// Lineas activas (no anuladas) de la cuenta, la mas reciente primero: es el orden en que se quitan las
    /// unidades. Una sola fuente para la lista de la cuenta y para la caja.
    /// </summary>
    public List<LineaActiva> LineasActivas() => Pedidos
        .OrderByDescending(p => p.Numero)
        .SelectMany(p => p.Lineas.Where(l => !l.Anulado).OrderByDescending(l => l.IdDetalle))
        .Select(l => new LineaActiva(l.IdDetalle, l.IdProducto, l.Producto, l.Cantidad, l.PrecioUnitario))
        .ToList();

    /// <summary>Lo que se cobra: las lineas activas sumadas por producto, ordenadas por nombre.</summary>
    public List<ProductoActivo> ProductosActivos() => LineasActivas()
        .GroupBy(l => l.IdProducto)
        .Select(g => new ProductoActivo(
            g.First().Nombre,
            g.Sum(l => l.Cantidad),
            g.Sum(l => l.Cantidad * l.Precio),
            g.Select(l => l.Precio).Distinct().Count() == 1 ? g.First().Precio : null,
            g.ToList()))
        .OrderBy(p => p.Nombre)
        .ToList();
}

/// <summary>Linea activa de la cuenta. Se serializa a JSON (data-lineas) para que el JS reparta lo que se quita.</summary>
public record LineaActiva(int IdDetalle, int IdProducto, string Nombre, int Cantidad, decimal Precio);

/// <summary>Un producto de la cuenta con sus lineas (la mas reciente primero); PrecioUnico es nulo si el precio cambio entre pedidos.</summary>
public record ProductoActivo(string Nombre, int Cantidad, decimal Subtotal, decimal? PrecioUnico, IReadOnlyList<LineaActiva> Lineas)
{
    public string LineasJson => LineaActivaJson.Serializar(Lineas);
}

public static class LineaActivaJson
{
    private static readonly System.Text.Json.JsonSerializerOptions Opciones = new(System.Text.Json.JsonSerializerDefaults.Web);

    /// <summary>camelCase, como lo lee site.js (idDetalle, idProducto, nombre, cantidad, precio).</summary>
    public static string Serializar(IEnumerable<LineaActiva> lineas) =>
        System.Text.Json.JsonSerializer.Serialize(lineas, Opciones);
}

public class PedidoCuentaApiResponse
{
    public int IdPedido { get; set; }
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "";
    public decimal Subtotal { get; set; }
    public List<LineaCuentaApiResponse> Lineas { get; set; } = [];
}

public class LineaCuentaApiResponse
{
    public int IdDetalle { get; set; }
    public int IdProducto { get; set; }
    public string Producto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public bool Anulado { get; set; }
}

public class CobroApiResponse
{
    public int? IdVenta { get; set; }
    public string? Folio { get; set; }
    public int? IdReserva { get; set; }
    public List<int> NumerosMesa { get; set; } = [];
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
}

public class MetodoPagoApiResponse
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = "";

    /// <summary>false si se deshabilito (solo viaja con todos=true): ya no se ofrece al cobrar pero sigue en ventas antiguas.</summary>
    public bool Activo { get; set; } = true;
}

public class CategoriaVentaApiResponse
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public List<ProductoVentaApiResponse> Productos { get; set; } = [];
}

public class ProductoVentaApiResponse
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
}

// ------------------------------------------------------------
// Modelos de las vistas parciales
// ------------------------------------------------------------

/// <summary>Cuerpo de la cuenta (_Cuenta): sirve igual en Mesas, Reservas del dia y Ventas.</summary>
public class CuentaViewModel
{
    public CuentaApiResponse Cuenta { get; set; } = new();
    public List<MetodoPagoApiResponse> MetodosPago { get; set; } = [];

    /// <summary>«Mesa 3» o «Mesas 3 + 4».</summary>
    public string EtiquetaMesa => Etiqueta(Cuenta.NumerosMesa);

    /// <summary>«N min» si es menor de una hora; si no «H h» o «H h MM». Nulo si el cliente aun no llega.</summary>
    public string? Sentados => Cuenta.FechaAtencion is { } llegada
        ? Duracion((DateTime.Now - llegada).TotalMinutes)
        : null;

    public static string Etiqueta(IReadOnlyList<int> numerosMesa) => numerosMesa.Count switch
    {
        0 => "",
        1 => $"Mesa {numerosMesa[0]}",
        _ => "Mesas " + string.Join(" + ", numerosMesa)
    };

    public static string Duracion(double minutos)
    {
        var total = (int)Math.Max(0, Math.Floor(minutos));
        if (total < 60) return $"{total} min";
        var horas = total / 60;
        var resto = total % 60;
        return resto == 0 ? $"{horas} h" : $"{horas} h {resto:00}";
    }

    public static string Dinero(decimal monto) => Colones.Formato(monto);
}

/// <summary>
/// Caja de venta (_Caja): el menu vendible y la cuenta a la que se agrega el pedido. En la venta para llevar la cuenta
/// va vacia y se agregan los metodos de pago.
/// </summary>
public class CajaViewModel
{
    public CuentaApiResponse Cuenta { get; set; } = new();
    public List<CategoriaVentaApiResponse> Categorias { get; set; } = [];

    /// <summary>Venta para llevar (HU-CON-003): sin cuenta de mesa, con metodo de pago y cobro dentro del ticket.</summary>
    public bool ParaLlevar { get; set; }
    public List<MetodoPagoApiResponse> MetodosPago { get; set; } = [];

    public string EtiquetaMesa => CuentaViewModel.Etiqueta(Cuenta.NumerosMesa);
}

// ------------------------------------------------------------
// HU-CON-004: Listado de ventas
// ------------------------------------------------------------

public class VentaListadoApiResponse
{
    public int IdVenta { get; set; }
    public string? Folio { get; set; }
    public DateTime FechaVenta { get; set; }
    public string TipoVenta { get; set; } = "";
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public bool? MetodoPagoActivo { get; set; }
    public string Estado { get; set; } = "";
}

public class FiltroVentasViewModel
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? TipoVenta { get; set; }
    public int? IdMetodoPago { get; set; }
    public string? Estado { get; set; }

    /// <summary>Las ventas del filtro (con el origen aplicado); el conteo por origen sale de las del filtro sin origen.</summary>
    public List<VentaListadoApiResponse> Ventas { get; set; } = [];
    public List<MetodoPagoApiResponse> MetodosPago { get; set; } = [];

    public const int TamanoPagina = 10;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Ventas.Count / (double)TamanoPagina));

    /// <summary>Las ventas de la pagina actual (10 como maximo).</summary>
    public List<VentaListadoApiResponse> PaginaActual => Ventas.Skip((Pagina - 1) * TamanoPagina).Take(TamanoPagina).ToList();

    /// <summary>Ventas del rango (con los demas filtros) por origen: Consumo (mesa) y Takeout (para llevar).</summary>
    public int ConteoConsumo { get; set; }
    public int ConteoParaLlevar { get; set; }
}

public class VentaDetalleApiResponse
{
    public int IdVenta { get; set; }
    public string Folio { get; set; } = "";
    public DateTime FechaVenta { get; set; }
    public string TipoVenta { get; set; } = "";
    public string Estado { get; set; } = "";
    public string? Motivo { get; set; }
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public bool? MetodoPagoActivo { get; set; }
    public string UsuarioRegistro { get; set; } = "";
    public List<LineaDetalleApiResponse> Lineas { get; set; } = [];
}

public class LineaDetalleApiResponse
{
    public string Producto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
