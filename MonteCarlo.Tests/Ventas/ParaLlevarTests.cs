using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Ventas;

/// <summary>HU-CON-003 (venta para llevar): criterios 1 (registro exitoso) y 2 (sin productos), y las reglas de dinero.</summary>
public class ParaLlevarTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private ParaLlevarRequest Solicitud(decimal totalEsperado, params (int IdProducto, int Cantidad)[] lineas) => new()
    {
        ClaveIdempotencia = Guid.NewGuid(),
        IdMetodoPago = Mundo.IdMetodoPago,
        TotalEsperado = totalEsperado,
        Lineas = lineas.Select(l => new LineaPedidoRequest { IdProducto = l.IdProducto, Cantidad = l.Cantidad }).ToList()
    };

    private Venta VentaGuardada(int idVenta)
    {
        using var db = Mundo.NuevoContexto();
        return db.Ventas.AsNoTracking().Include(v => v.Detalles).Single(v => v.IdVenta == idVenta);
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_conProductosYMetodoDePago_registraVentaCobradaSinReservaNiMesa()
    {
        var cafe = Mundo.CrearProducto(precio: 1500m);
        var pan = Mundo.CrearProducto(precio: 800m);

        var r = await S.Ventas.RegistrarParaLlevarAsync(Solicitud(3 * 1500m + 800m, (cafe.IdProducto, 3), (pan.IdProducto, 1)), Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Equal(5300m, r.Data!.Total);
        Assert.Null(r.Data.IdReserva);
        Assert.Empty(r.Data.NumerosMesa);
        var venta = VentaGuardada(r.Data.IdVenta!.Value);
        Assert.Equal(VentaTipos.Takeout, venta.TipoVenta);
        Assert.Equal(VentaEstados.Cobrada, venta.Estado);
        Assert.Null(venta.IdReserva);
        Assert.Equal(5300m, venta.Total);
        Assert.Equal(Mundo.IdMetodoPago, venta.IdMetodoPago);
        Assert.Equal(Mundo.IdUsuario, venta.IdUsuarioCobro);
        Assert.NotNull(venta.FechaCobro);
        Assert.Equal(2, venta.Detalles.Count);
        Assert.Contains(venta.Detalles, d => d.IdProducto == cafe.IdProducto && d.Cantidad == 3 && d.PrecioUnitario == 1500m);
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_laVentaApareceEnElListadoDeVentasComoTakeoutCobrada()
    {
        var producto = Mundo.CrearProducto(precio: 1200m);
        var r = await S.Ventas.RegistrarParaLlevarAsync(Solicitud(1200m, (producto.IdProducto, 1)), Mundo.IdUsuario);

        var listado = (await S.Ventas.ObtenerListadoAsync(new FiltroVentasRequest { TipoVenta = VentaTipos.Takeout, Estado = VentaEstados.Cobrada })).Data!;

        var fila = Assert.Single(listado, v => v.IdVenta == r.Data!.IdVenta);
        Assert.Equal(1200m, fila.Total);
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_mismaClave_registraUnaSolaVenta()
    {
        var producto = Mundo.CrearProducto(precio: 1000m);
        var solicitud = Solicitud(2000m, (producto.IdProducto, 2));

        var primera = await S.Ventas.RegistrarParaLlevarAsync(solicitud, Mundo.IdUsuario);
        var reintento = await S.Ventas.RegistrarParaLlevarAsync(solicitud, Mundo.IdUsuario);

        Assert.True(primera.Success, primera.Message);
        Assert.True(reintento.Success, reintento.Message);
        Assert.Equal(primera.Data!.IdVenta, reintento.Data!.IdVenta);
        Assert.Equal(primera.Data.Folio, reintento.Data.Folio);
        using var db = Mundo.NuevoContexto();
        Assert.Equal(1, db.PedidosVenta.Count(p => p.ClaveIdempotencia == solicitud.ClaveIdempotencia));
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "2")]
    public async Task ParaLlevar_sinProductos_devuelve400QueExigeAlMenosUno()
    {
        var r = await S.Ventas.RegistrarParaLlevarAsync(Solicitud(0m), Mundo.IdUsuario);

        Assert.Equal(400, r.StatusCode);
        Assert.Contains("al menos un producto", r.Message);
        Assert.Contains("la venta", r.Message);
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_totalQueNoCoincide_devuelve409YNoRegistraNada()
    {
        var producto = Mundo.CrearProducto(precio: 1000m);
        var solicitud = Solicitud(1000m, (producto.IdProducto, 1));
        Mundo.CambiarPrecio(producto.IdProducto, 1200m);   // el precio cambio despues de armar el pedido

        var r = await S.Ventas.RegistrarParaLlevarAsync(solicitud, Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("precios cambiaron", r.Message);
        using var db = Mundo.NuevoContexto();
        Assert.Equal(0, db.PedidosVenta.Count(p => p.ClaveIdempotencia == solicitud.ClaveIdempotencia));
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_productoNoDisponible_devuelve409()
    {
        var producto = Mundo.CrearProducto(precio: 1000m, disponible: false);

        var r = await S.Ventas.RegistrarParaLlevarAsync(Solicitud(1000m, (producto.IdProducto, 1)), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("ya no esta disponible", r.Message);
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_metodoDePagoInactivoOInexistente_devuelve400()
    {
        var producto = Mundo.CrearProducto(precio: 1000m);
        int idInactivo;
        using (var db = Mundo.NuevoContexto())
        {
            var metodo = new MetodoPago { Nombre = $"Inactivo{Guid.NewGuid():N}"[..20], Activo = false };
            db.MetodosPago.Add(metodo);
            db.SaveChanges();
            idInactivo = metodo.IdMetodoPago;
        }

        var inactivo = Solicitud(1000m, (producto.IdProducto, 1));
        inactivo.IdMetodoPago = idInactivo;
        var inexistente = Solicitud(1000m, (producto.IdProducto, 1));
        inexistente.IdMetodoPago = int.MaxValue;

        Assert.Equal(400, (await S.Ventas.RegistrarParaLlevarAsync(inactivo, Mundo.IdUsuario)).StatusCode);
        Assert.Equal(400, (await S.Ventas.RegistrarParaLlevarAsync(inexistente, Mundo.IdUsuario)).StatusCode);
    }

    [SqlFact]
    [Trait("HU", "CON-003"), Trait("Escenario", "1")]
    public async Task ParaLlevar_unaClaveDeUnPedidoDeMesa_devuelve409()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(precio: 1000m);
        var clave = Guid.NewGuid();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Atajos.PedidoConClave(clave, (producto.IdProducto, 1)), Mundo.IdUsuario);
        var solicitud = Solicitud(1000m, (producto.IdProducto, 1));
        solicitud.ClaveIdempotencia = clave;

        var r = await S.Ventas.RegistrarParaLlevarAsync(solicitud, Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
    }
}
