using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

/// <summary>Resumen de una cuenta abierta para pintar las mesas sin cargar sus lineas.</summary>
public record CuentaAbiertaResumen(int IdReserva, int IdVenta, decimal Total, int Productos, string Version);

public interface IVentasRepository
{
    /// <summary>
    /// La venta (cuenta) de una reserva, con pedidos (y quien los registro) y lineas (con su producto).
    /// Entidad rastreada: sirve para leer y para modificar.
    /// </summary>
    Task<Venta?> ObtenerPorReservaAsync(int idReserva);

    /// <summary>El pedido con esa clave de idempotencia (con su venta), si ya se registro.</summary>
    Task<PedidoVenta?> ObtenerPedidoPorClaveAsync(Guid clave);

    /// <summary>Una linea con su venta y las lineas de la venta (rastreadas), para quitarla y recalcular el total.</summary>
    Task<DetalleVenta?> ObtenerLineaAsync(int idDetalle);

    /// <summary>true si la reserva tiene una cuenta abierta (con o sin productos activos).</summary>
    Task<bool> TieneCuentaAbiertaAsync(int idReserva);

    /// <summary>Cuentas abiertas de esas reservas en una sola consulta (total, unidades y version).</summary>
    Task<Dictionary<int, CuentaAbiertaResumen>> ObtenerResumenCuentasAbiertasAsync(IReadOnlyCollection<int> idsReserva);

    /// <summary>
    /// Mesas ocupadas por una reserva Confirmada con cuenta abierta, de cualquier dia (la cuenta mantiene la mesa
    /// ocupada aunque el turno ya haya terminado), con el momento en que se sento el cliente (la llegada, o la
    /// apertura de la cuenta si no hay llegada registrada). Sirve para estimar cuando se libera cada mesa.
    /// </summary>
    Task<Dictionary<int, DateTime>> ObtenerMesasConCuentaAbiertaAsync();

    void Agregar(Venta venta);

    /// <summary>Marca el total como modificado para que RowVersion cambie aunque la suma no varie.</summary>
    void MarcarModificada(Venta venta);

    Task<List<Venta>> ObtenerConFiltrosAsync(FiltroVentasRequest filtro);


    Task<Venta?> ObtenerDetallePorIdAsync(int idVenta);

    Task GuardarCambiosAsync();
}
