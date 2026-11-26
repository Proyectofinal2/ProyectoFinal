using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

public class IvaCuentaTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    [SqlFact]
    public async Task PrimerPedidoConEstimacionAnteriorNoAbreLaCuenta()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1000m);
        var pedido = Pedido((producto.IdProducto, 1));
        pedido.TotalEsperado = 1000m;
        var resultado = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, pedido, Mundo.IdUsuario);
        Assert.Equal(409, resultado.StatusCode);
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
        pedido.TotalEsperado = 1130m;
        var confirmado = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, pedido, Mundo.IdUsuario);
        Assert.True(confirmado.Success, confirmado.Message);
        Assert.Equal(1130m, confirmado.Data!.Total);
    }

    [SqlFact]
    public async Task CambiarTasaNoAlteraCuentaAbiertaNiSusPedidos()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1000m);
        var primero = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        Assert.True(primero.Success, primero.Message);
        try
        {
            await using (var db = Mundo.NuevoContexto())
            {
                (await db.ConfiguracionesSistema.SingleAsync(c => c.Clave == "TasaIva")).Valor = "20";
                await db.SaveChangesAsync();
            }
            using var nueva = Mundo.NuevaSesion();
            var segundo = await nueva.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 2)), Mundo.IdUsuario);
            Assert.True(segundo.Success, segundo.Message);
            Assert.Equal(13m, segundo.Data!.TasaIvaAplicada);
            Assert.Equal(3000m, segundo.Data.Subtotal);
            Assert.Equal(390m, segundo.Data.ImporteIva);
            Assert.Equal(3390m, segundo.Data.Total);
        }
        finally
        {
            await using var db = Mundo.NuevoContexto();
            (await db.ConfiguracionesSistema.SingleAsync(c => c.Clave == "TasaIva")).Valor = "13";
            await db.SaveChangesAsync();
        }
    }

    [SqlFact]
    public async Task CuentaAnteriorConservaNulosAlAgregarYAnular()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1000m);
        var primero = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 2)), Mundo.IdUsuario);
        Assert.True(primero.Success, primero.Message);
        await using (var db = Mundo.NuevoContexto())
        {
            var venta = await db.Ventas.SingleAsync(v => v.IdReserva == reserva.IdReserva);
            venta.Subtotal = null;
            venta.TasaIvaAplicada = null;
            venta.ImporteIva = null;
            venta.Total = 2000m;
            await db.SaveChangesAsync();
        }
        using var sesion = Mundo.NuevaSesion();
        var segundo = await sesion.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        Assert.True(segundo.Success, segundo.Message);
        Assert.Null(segundo.Data!.TasaIvaAplicada);
        Assert.Null(segundo.Data.Subtotal);
        Assert.Equal(3000m, segundo.Data.Total);
        var linea = segundo.Data.Pedidos.First().Lineas.Single();
        var quitar = await sesion.Ventas.AnularLineaAsync(linea.IdDetalle, new AnularLineaRequest { Cantidad = 1 }, Mundo.IdUsuario);
        Assert.True(quitar.Success, quitar.Message);
        Assert.Null(quitar.Data!.ImporteIva);
        Assert.Equal(2000m, quitar.Data.Total);
        var cobro = await sesion.Ventas.CobrarAsync(reserva.IdReserva, new CobrarRequest
        {
            IdMetodoPago = Mundo.IdMetodoPago, Version = quitar.Data.Version, TotalEsperado = 2000m
        }, Mundo.IdUsuario);
        Assert.True(cobro.Success, cobro.Message);
        using var consulta = Mundo.NuevaSesion();
        var detalle = await consulta.Ventas.ObtenerDetalleAsync(cobro.Data!.IdVenta!.Value);
        Assert.True(detalle.Success, detalle.Message);
        Assert.Null(detalle.Data!.Subtotal);
        Assert.Null(detalle.Data.TasaIvaAplicada);
        Assert.Null(detalle.Data.ImporteIva);
        Assert.Equal(2000m, detalle.Data.Total);
    }
}
