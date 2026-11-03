using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

/// <summary>
/// Que ofrece la cuenta (PuedeAgregar / PuedeCobrar / PuedeLiberarSinConsumo / Bloqueo) en cada situacion.
/// Protege ConstruirCuenta y CalcularBloqueo antes de simplificarlos.
/// </summary>
public class CuentaEstadoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "7")]
    public async Task Cuenta_reservaSentadaSinPedidos_estadoSinPedidosPuedeAgregarYLiberar()
    {
        var (reserva, mesa) = Mundo.CrearReservaSentada();

        var c = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!;

        Assert.Equal("SinPedidos", c.Estado);
        Assert.Null(c.IdVenta);
        Assert.Null(c.Folio);
        Assert.Null(c.Version);
        Assert.True(c.PuedeAgregar);
        Assert.False(c.PuedeCobrar);
        Assert.True(c.PuedeLiberarSinConsumo);
        Assert.Null(c.Bloqueo);
        Assert.Equal([mesa.NumeroMesa], c.NumerosMesa);
        Assert.Equal("Confirmada", c.EstadoReserva);
    }


    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "7")]
    public async Task Cuenta_conTodasLasLineasQuitadas_noPuedeCobrarPeroSiLiberar()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto();
        var abierta = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        await S.Ventas.AnularLineaAsync(abierta.Data!.Pedidos[0].Lineas[0].IdDetalle, new AnularLineaRequest(), Mundo.IdUsuario);

        var c = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!;

        Assert.Equal(0m, c.Total);
        Assert.Equal(0, c.TotalProductos);
        Assert.False(c.PuedeCobrar);
        Assert.True(c.PuedeLiberarSinConsumo);
    }




}
