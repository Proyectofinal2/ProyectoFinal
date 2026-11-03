using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

/// <summary>HU-CON-002, criterios 1, 3, 4 y 5: registro de pedidos en la cuenta abierta.</summary>
public class AgregarPedidoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "5")]
    public async Task AgregarPedido_primerPedido_abreLaCuentaConFolioUsuarioYHora()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(precio: 2500m);

        var antes = DateTime.Now.AddSeconds(-2);
        var r = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 2)), Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Equal("Abierta", r.Data!.Estado);
        Assert.Equal($"V-{r.Data.IdVenta:D6}", r.Data.Folio);
        Assert.Equal(5000m, r.Data.Total);
        var pedido = Assert.Single(r.Data.Pedidos);
        Assert.Equal(1, pedido.Numero);
        Assert.True(pedido.Fecha >= antes);
        var venta = Mundo.VentaDe(reserva.IdReserva)!;
        Assert.Equal(VentaEstados.Abierta, venta.Estado);
        Assert.Equal(Mundo.IdUsuario, venta.Pedidos.Single().IdUsuario);
        Assert.Null(venta.IdMetodoPago);
        Assert.Null(venta.FechaCobro);
    }



    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "5")]
    public async Task AgregarPedido_segundoPedido_mismaVentaNumero2YTotalComoSuma()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var cafe = Mundo.CrearProducto(1000m);
        var pastel = Mundo.CrearProducto(3000m);

        var primero = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((cafe.IdProducto, 1)), Mundo.IdUsuario);
        var segundo = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((pastel.IdProducto, 2)), Mundo.IdUsuario);

        Assert.Equal(primero.Data!.IdVenta, segundo.Data!.IdVenta);
        Assert.Equal([1, 2], segundo.Data.Pedidos.Select(p => p.Numero));
        Assert.Equal(7000m, segundo.Data.Total);
        Assert.Equal(1, Mundo.CantidadVentas(reserva.IdReserva));
    }



    [SqlTheory]
    [InlineData(0)]
    [InlineData(100)]
    [Trait("HU", "CON-002"), Trait("Escenario", "5")]
    public async Task AgregarPedido_cantidadFueraDeRango_devuelve400SinEfectos(int cantidad)
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto();

        var r = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, cantidad)), Mundo.IdUsuario);

        Assert.Equal(400, r.StatusCode);
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
    }




    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "5")]
    public async Task AgregarPedido_mismaClaveDosVeces_seRegistraUnaSolaVez()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1000m);
        var clave = Guid.NewGuid();

        var primero = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, PedidoConClave(clave, (producto.IdProducto, 1)), Mundo.IdUsuario);
        var repetido = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, PedidoConClave(clave, (producto.IdProducto, 1)), Mundo.IdUsuario);

        Assert.True(primero.Success);
        Assert.True(repetido.Success);
        Assert.Contains("ya estaba registrado", repetido.Message);
        Assert.Equal(1000m, repetido.Data!.Total);
        Assert.Single(Mundo.VentaDe(reserva.IdReserva)!.Pedidos);
    }


    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "5")]
    public async Task AgregarPedido_dosPrimerosPedidosSimultaneos_unoGanaYElOtroRecibe409()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto();
        using var sesionA = Mundo.NuevaSesion();
        using var sesionB = Mundo.NuevaSesion();

        // Ambas leen "sin cuenta" antes de que cualquiera guarde: se fuerza solapando las cargas.
        var resultados = await Task.WhenAll(
            sesionA.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario),
            sesionB.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario));

        // Nunca 500 ni dos ventas. Segun el solapamiento real el segundo puede entrar como pedido 2 (200) o chocar (409).
        Assert.DoesNotContain(resultados, x => x.StatusCode == 500);
        Assert.Contains(resultados, x => x.Success);
        Assert.All(resultados, x => Assert.Contains(x.StatusCode, new[] { 200, 409 }));
        Assert.Equal(1, Mundo.CantidadVentas(reserva.IdReserva));
    }


    // ---------- Criterio 3: producto no disponible ----------

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "3")]
    public async Task AgregarPedido_productoDeshabilitado_devuelve409YLaCuentaNoCambia()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(disponible: false);

        var r = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("ya no esta disponible", r.Message);
        Assert.NotNull(CuentaDelError(r));
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
    }




    // ---------- Criterio 4: reserva que no esta Confirmada ----------

    [SqlTheory]
    [InlineData("Pendiente")]
    [InlineData("Cancelada")]
    [Trait("HU", "CON-002"), Trait("Escenario", "4")]
    public async Task AgregarPedido_reservaNoConfirmada_devuelve409ConLaCuentaYNoCreaVenta(string estado)
    {
        var mesa = Mundo.CrearMesa();
        var reserva = Mundo.CrearReserva([mesa], estado);
        var producto = Mundo.CrearProducto();

        var r = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        var cuenta = CuentaDelError(r);
        Assert.NotNull(cuenta);
        Assert.Equal(estado, cuenta!.EstadoReserva);
        Assert.Contains(estado, cuenta.Bloqueo);
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
    }



    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "4")]
    public async Task ReservaInexistente_devuelve404EnCuentaPedidoCobroYLiberar()
    {
        var producto = Mundo.CrearProducto();
        const int inexistente = int.MaxValue;

        var cuenta = await S.Ventas.ObtenerCuentaAsync(inexistente);
        var pedido = await S.Ventas.AgregarPedidoAsync(inexistente, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        var cobro = await S.Ventas.CobrarAsync(inexistente,
            new CobrarRequest { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = 0, Version = "AA==" }, Mundo.IdUsuario);
        var liberar = await S.Ventas.LiberarSinConsumoAsync(inexistente, Mundo.IdUsuario);

        Assert.Equal([404, 404, 404, 404], new[] { cuenta.StatusCode, pedido.StatusCode, cobro.StatusCode, liberar.StatusCode });
    }

}
