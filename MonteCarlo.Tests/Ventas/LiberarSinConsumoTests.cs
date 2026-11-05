using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

/// <summary>HU-CON-002, criterio 2 (cierre sin consumo) y criterio 11 (esperando llegada).</summary>
public class LiberarSinConsumoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "2")]
    public async Task LiberarSinConsumo_cuentaQueNuncaTuvoPedidos_finalizaYLiberaSinCrearVenta()
    {
        var (reserva, mesa) = Mundo.CrearReservaSentada();

        var r = await S.Ventas.LiberarSinConsumoAsync(reserva.IdReserva, Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Equal(0m, r.Data!.Total);
        Assert.Null(r.Data.MetodoPago);
        Assert.Null(r.Data.IdVenta);
        Assert.Null(r.Data.Folio);
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
        var guardada = Mundo.ReservaActual(reserva.IdReserva);
        Assert.Equal("Finalizada", guardada.EstadoReserva.Nombre);
        Assert.Equal("Liberada sin consumo", guardada.MotivoCancelacion);
        Assert.Equal("Disponible", (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == mesa.IdMesa).Estado);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "2")]
    public async Task ObtenerCuenta_reservaFinalizadaSinVenta_noTieneCuentaYNoPermiteAgregar()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        await S.Ventas.LiberarSinConsumoAsync(reserva.IdReserva, Mundo.IdUsuario);

        var cuenta = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!;

        Assert.Null(cuenta.IdVenta);
        Assert.Equal("SinPedidos", cuenta.Estado);
        Assert.False(cuenta.PuedeAgregar);
        Assert.NotNull(cuenta.Bloqueo);
    }


    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "2")]
    public async Task LiberarSinConsumo_conProductosActivos_devuelve409QueMandaACobrar()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        var r = await S.Ventas.LiberarSinConsumoAsync(reserva.IdReserva, Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("cobrala", r.Message);
        Assert.Equal(VentaEstados.Abierta, Mundo.VentaDe(reserva.IdReserva)!.Estado);
        Assert.Equal("Confirmada", Mundo.ReservaActual(reserva.IdReserva).EstadoReserva.Nombre);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "11")]
    public async Task LiberarSinConsumo_reservaSinLlegada_devuelve409ElClienteNoHaLlegado()
    {
        var reserva = Mundo.CrearReserva(atendida: false);

        var r = await S.Ventas.LiberarSinConsumoAsync(reserva.IdReserva, Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("todavia no ha llegado", r.Message);
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
    }


    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "2")]
    public async Task LiberarSinConsumo_cuentaAbiertaConTodosLosProductosQuitados_cierraLaCuentaEnCero()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        var linea = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!.Pedidos.Single().Lineas.Single();
        await S.Ventas.AnularLineaAsync(linea.IdDetalle, new AnularLineaRequest(), Mundo.IdUsuario);

        var r = await S.Ventas.LiberarSinConsumoAsync(reserva.IdReserva, Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        var venta = Mundo.VentaDe(reserva.IdReserva)!;
        Assert.Equal(VentaEstados.Cobrada, venta.Estado);
        Assert.Equal(0m, venta.Total);
        Assert.Null(venta.IdMetodoPago);
    }

    // ---------- PUT api/mesas/{id}/liberar ----------



}
