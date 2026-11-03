using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

/// <summary>HU-CON-002, criterios 1 y 9: cobro y cuenta modificada durante el cobro.</summary>
public class CobrarTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private CobrarRequest Cobro(CuentaResponse cuenta, int? metodo = null) => new()
    {
        IdMetodoPago = metodo ?? Mundo.IdMetodoPago, TotalEsperado = cuenta.Total, Version = cuenta.Version
    };

    private async Task<(int IdReserva, int IdMesa, CuentaResponse Cuenta)> CuentaConAsync(decimal precio = 1000m, int cantidad = 2)
    {
        var (reserva, mesa) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(precio);
        var r = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, cantidad)), Mundo.IdUsuario);
        return (reserva.IdReserva, mesa.IdMesa, r.Data!);
    }

    // ---------- Criterio 1 ----------

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "1")]
    public async Task Cobrar_cuentaConPedidos_marcaCobradaFinalizaLaReservaYLiberaLaMesa()
    {
        var (idReserva, idMesa, cuenta) = await CuentaConAsync(precio: 2000m, cantidad: 2);
        var segundo = await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((cuenta.Pedidos[0].Lineas[0].IdProducto, 1)), Mundo.IdUsuario);
        var vista = segundo.Data!;
        Assert.Equal("Ocupada", (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == idMesa).Estado);

        var antes = DateTime.Now.AddSeconds(-2);
        var r = await S.Ventas.CobrarAsync(idReserva, Cobro(vista), Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Equal(6000m, r.Data!.Total);
        Assert.Equal($"V-{r.Data.IdVenta:D6}", r.Data.Folio);
        var venta = Mundo.VentaDe(idReserva)!;
        Assert.Equal(VentaEstados.Cobrada, venta.Estado);
        Assert.Equal(Mundo.IdMetodoPago, venta.IdMetodoPago);
        Assert.Equal(Mundo.IdUsuario, venta.IdUsuarioCobro);
        Assert.True(venta.FechaCobro >= antes);
        Assert.Equal("Finalizada", Mundo.ReservaActual(idReserva).EstadoReserva.Nombre);
        Assert.Equal("Disponible", (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == idMesa).Estado);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "1")]
    public async Task Cobrar_precioCongeladoAlAgregar_cambiarElCatalogoNoAfectaLaCuentaPeroSiAlPedidoNuevo()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1000m);
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        Mundo.CambiarPrecio(producto.IdProducto, 1800m);
        var cuentaMismoPrecio = await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva);
        var conPedidoNuevo = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        Assert.Equal(1000m, cuentaMismoPrecio.Data!.Total);
        Assert.Equal(2800m, conPedidoNuevo.Data!.Total);
        Assert.Equal([1000m, 1800m], conPedidoNuevo.Data.Pedidos.SelectMany(p => p.Lineas).Select(l => l.PrecioUnitario));
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "1")]
    public async Task Cobrar_reservaConDosMesasUnidas_unaSolaCuentaYLiberaAmbas()
    {
        var mesaA = Mundo.CrearMesa(2);
        var mesaB = Mundo.CrearMesa(2);
        var reserva = Mundo.CrearReserva([mesaA, mesaB], personas: 4);
        var producto = Mundo.CrearProducto(1000m);
        var abierta = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        var vivas = (await S.Mesas.ListarAsync(false)).Data!.Where(m => m.IdMesa == mesaA.IdMesa || m.IdMesa == mesaB.IdMesa).ToList();
        Assert.All(vivas, m => Assert.Equal(1000m, m.Cuenta!.Total));
        Assert.Equal(abierta.Data!.Version, vivas[0].Cuenta!.Version);

        await S.Ventas.CobrarAsync(reserva.IdReserva, Cobro(abierta.Data), Mundo.IdUsuario);

        Assert.Equal(1, Mundo.CantidadVentas(reserva.IdReserva));
        var despues = (await S.Mesas.ListarAsync(false)).Data!.Where(m => m.IdMesa == mesaA.IdMesa || m.IdMesa == mesaB.IdMesa);
        Assert.All(despues, m => Assert.Equal("Disponible", m.Estado));
    }

    // ---------- Criterio 9 ----------

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "9")]
    public async Task Cobrar_conTotalDesactualizado_devuelve409ConLaCuentaActualYNoCambiaLaBase()
    {
        var (idReserva, _, vista) = await CuentaConAsync();
        var otro = Mundo.CrearProducto(500m);
        await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((otro.IdProducto, 1)), Mundo.IdUsuario);

        var r = await S.Ventas.CobrarAsync(idReserva, Cobro(vista), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Equal(2500m, CuentaDelError(r)!.Total);
        Assert.Contains("el total ahora es", r.Message);
        var venta = Mundo.VentaDe(idReserva)!;
        Assert.Equal(VentaEstados.Abierta, venta.Estado);
        Assert.Null(venta.FechaCobro);
        Assert.Equal("Confirmada", Mundo.ReservaActual(idReserva).EstadoReserva.Nombre);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "9")]
    public async Task Cobrar_versionVieja_conElMismoTotal_devuelve409()
    {
        // Se quita un producto y se agrega otro del mismo precio: el total no cambia, la version si.
        var (idReserva, _, vista) = await CuentaConAsync(precio: 1000m, cantidad: 1);
        var igual = Mundo.CrearProducto(1000m);
        await S.Ventas.AnularLineaAsync(vista.Pedidos[0].Lineas[0].IdDetalle, new AnularLineaRequest(), Mundo.IdUsuario);
        await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((igual.IdProducto, 1)), Mundo.IdUsuario);
        Assert.Equal(1000m, (await S.Ventas.ObtenerCuentaAsync(idReserva)).Data!.Total);

        var r = await S.Ventas.CobrarAsync(idReserva, Cobro(vista), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Equal(VentaEstados.Abierta, Mundo.VentaDe(idReserva)!.Estado);
    }




    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "9")]
    public async Task Cobrar_dosCobrosSimultaneos_unoGanaYElOtroRecibe409()
    {
        var (idReserva, _, vista) = await CuentaConAsync();
        using var sesionA = Mundo.NuevaSesion();
        using var sesionB = Mundo.NuevaSesion();

        var resultados = await Task.WhenAll(
            sesionA.Ventas.CobrarAsync(idReserva, Cobro(vista), Mundo.IdUsuario),
            sesionB.Ventas.CobrarAsync(idReserva, Cobro(vista), Mundo.IdUsuario));

        Assert.Single(resultados, r => r.Success);
        Assert.Single(resultados, r => r.StatusCode == 409);
        using var db = Mundo.NuevoContexto();
        Assert.Equal(1, db.Ventas.Count(v => v.IdReserva == idReserva && v.Estado == VentaEstados.Cobrada));
    }




}
