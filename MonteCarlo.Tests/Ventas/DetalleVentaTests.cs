using Microsoft.Extensions.Logging.Abstractions;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services;
using MonteCarlo.API.Services.Interfaces;
using MonteCarlo.Tests.Infra;
using NSubstitute;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

public class DetalleVentaEstadosTests
{
    [Theory]
    [InlineData(VentaEstados.Abierta)]
    [InlineData(VentaEstados.Cobrada)]
    [InlineData(VentaEstados.Anulada)]
    [InlineData(VentaEstados.Abandonada)]
    public async Task Detalle_excluyeAnuladasEnCadaEstado_sinModificarLaColeccion(string estado)
    {
        var anulada = new DetalleVenta { Anulado = true, Producto = new Producto { Nombre = "Quitado" } };
        var activa = new DetalleVenta { Cantidad = 2, PrecioUnitario = 1000m, Producto = new Producto { Nombre = "Activo" } };
        var venta = new Venta
        {
            IdVenta = 1, Estado = estado, TipoVenta = VentaTipos.Consumo, Total = 2000m,
            UsuarioRegistro = new Usuario { NombreCompleto = "Usuario" }, Detalles = [anulada, activa]
        };
        var repo = Substitute.For<IVentasRepository>();
        repo.ObtenerDetallePorIdAsync(1).Returns(venta);
        var servicio = new VentasService(repo, Substitute.For<IReservasRepository>(),
            Substitute.For<IProductosRepository>(), Substitute.For<IMetodosPagoRepository>(),
            Substitute.For<IConfiguracionService>(), NullLogger<VentasService>.Instance);

        var resultado = await servicio.ObtenerDetalleAsync(1);

        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal(estado, resultado.Data!.Estado);
        Assert.Equal(2000m, resultado.Data.Total);
        Assert.Equal("Activo", Assert.Single(resultado.Data.Lineas).Producto);
        Assert.Equal(2, venta.Detalles.Count);
        Assert.Contains(anulada, venta.Detalles);
    }
}

public class DetalleVentaTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    [SqlTheory]
    [InlineData("13", 4500, 1170, 10170)]
    [InlineData("7.25", 100.25, 14.54, 215.04)]
    [InlineData("0", 100.25, 0, 200.50)]
    public async Task Detalle_conservaImportesTrasCambiarTasaYPrecio(
        string tasa, decimal precio, decimal iva, decimal total)
    {
        var producto = Mundo.CrearProducto(precio);
        using var db = Mundo.NuevoContexto();
        var parametro = db.ConfiguracionesSistema.Single(c => c.Clave == "TasaIva");
        var tasaAnterior = parametro.Valor;
        try
        {
            parametro.Valor = tasa;
            await db.SaveChangesAsync();
            var cobro = await S.Ventas.RegistrarParaLlevarAsync(new ParaLlevarRequest
            {
                ClaveIdempotencia = Guid.NewGuid(), IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = total,
                Lineas = [new LineaPedidoRequest { IdProducto = producto.IdProducto, Cantidad = 2 }]
            }, Mundo.IdUsuario);
            Assert.True(cobro.Success, cobro.Message);
            parametro.Valor = "20";
            db.Productos.Single(p => p.IdProducto == producto.IdProducto).Precio = 9999m;
            await db.SaveChangesAsync();

            using var consulta = Mundo.NuevaSesion();
            var resultado = await consulta.Ventas.ObtenerDetalleAsync(cobro.Data!.IdVenta!.Value);
            Assert.True(resultado.Success, resultado.Message);
            var detalle = resultado.Data!;
            Assert.Equal(decimal.Parse(tasa, System.Globalization.CultureInfo.InvariantCulture), detalle.TasaIvaAplicada);
            Assert.Equal(iva, detalle.ImporteIva);
            Assert.Equal(total, detalle.Total);
            Assert.Equal(2 * precio, detalle.Subtotal);
            var linea = Assert.Single(detalle.Lineas);
            Assert.Equal(precio, linea.PrecioUnitario);
            Assert.Equal(detalle.Subtotal, linea.Subtotal);
        }
        finally
        {
            parametro.Valor = tasaAnterior;
            await db.SaveChangesAsync();
        }
    }

    [SqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detalle_trasAnularCompletaEnMultiplesPedidos_conservaAuditoriaYTotal(bool walkIn)
    {
        int idReserva;
        if (walkIn)
        {
            var mesa = Mundo.CrearMesa();
            var creada = await S.Reservas.CrearWalkInAsync(new CrearReservaWalkInRequest { CantidadPersonas = 2, IdsMesa = [mesa.IdMesa] });
            Assert.True(creada.Success, creada.Message);
            idReserva = creada.Data!.IdReserva;
        }
        else idReserva = Mundo.CrearReservaSentada().Reserva.IdReserva;

        var quitado = Mundo.CrearProducto(1500m);
        var activo = Mundo.CrearProducto(500m);
        var primero = await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((quitado.IdProducto, 2)), Mundo.IdUsuario);
        Assert.True(primero.Success, primero.Message);
        var segundo = await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((activo.IdProducto, 2)), Mundo.IdUsuario);
        Assert.True(segundo.Success, segundo.Message);
        var linea = primero.Data!.Pedidos.SelectMany(p => p.Lineas).Single();
        var anulacion = await S.Ventas.AnularLineaAsync(linea.IdDetalle, new AnularLineaRequest { Motivo = "Correccion" }, Mundo.IdUsuario);
        Assert.True(anulacion.Success, anulacion.Message);
        var cuenta = anulacion.Data!;
        Assert.Equal(2, cuenta.Pedidos.Count);
        Assert.Single(cuenta.Pedidos.SelectMany(p => p.Lineas), l => l.Anulado);
        var cobro = await S.Ventas.CobrarAsync(idReserva, new CobrarRequest
        { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = cuenta.Total, Version = cuenta.Version }, Mundo.IdUsuario);
        Assert.True(cobro.Success, cobro.Message);

        using var consulta = Mundo.NuevaSesion();
        var detalle = await consulta.Ventas.ObtenerDetalleAsync(cobro.Data!.IdVenta!.Value);
        Assert.True(detalle.Success, detalle.Message);
        var visible = Assert.Single(detalle.Data!.Lineas);
        Assert.Equal(activo.Nombre, visible.Producto);
        Assert.Equal(1130m, detalle.Data.Total);
        Assert.Equal(detalle.Data.Subtotal, detalle.Data.Lineas.Sum(l => l.Subtotal));
        Assert.Equal(130m, detalle.Data.ImporteIva);
        var registrada = Mundo.VentaDe(idReserva)!;
        var auditada = Assert.Single(registrada.Detalles, d => d.Anulado);
        Assert.Equal("Correccion", auditada.MotivoAnulacion);
        Assert.Equal(Mundo.IdUsuario, auditada.IdUsuarioAnulacion);
        Assert.NotNull(auditada.FechaAnulacion);
        Assert.Equal(2, registrada.Pedidos.Count);
        var historial = await consulta.Ventas.ObtenerCuentaAsync(idReserva);
        Assert.Contains(historial.Data!.Pedidos.SelectMany(p => p.Lineas), l => l.Anulado);
    }

    [SqlFact]
    public async Task Detalle_trasAnulacionParcial_muestraSoloCantidadRestante()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1000m);
        var pedido = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 3)), Mundo.IdUsuario);
        Assert.True(pedido.Success, pedido.Message);
        var linea = pedido.Data!.Pedidos.SelectMany(p => p.Lineas).Single();
        var anulacion = await S.Ventas.AnularLineaAsync(linea.IdDetalle, new AnularLineaRequest { Cantidad = 1 }, Mundo.IdUsuario);
        Assert.True(anulacion.Success, anulacion.Message);
        var cuenta = anulacion.Data!;
        var cobro = await S.Ventas.CobrarAsync(reserva.IdReserva, new CobrarRequest
        { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = cuenta.Total, Version = cuenta.Version }, Mundo.IdUsuario);
        Assert.True(cobro.Success, cobro.Message);
        using var consulta = Mundo.NuevaSesion();
        var detalle = await consulta.Ventas.ObtenerDetalleAsync(cobro.Data!.IdVenta!.Value);
        Assert.True(detalle.Success, detalle.Message);
        var visible = Assert.Single(detalle.Data!.Lineas);
        Assert.Equal(2, visible.Cantidad);
        Assert.Equal(2000m, visible.Subtotal);
        Assert.Equal(detalle.Data.Subtotal, visible.Subtotal);
        Assert.Equal(2260m, detalle.Data.Total);
        var guardada = Mundo.VentaDe(reserva.IdReserva)!;
        Assert.Equal(3, Assert.Single(guardada.Detalles, d => d.Anulado).Cantidad);
        Assert.Equal(2, Assert.Single(guardada.Detalles, d => !d.Anulado).Cantidad);
    }

    [SqlFact]
    public async Task Detalle_paraLlevar_muestraLosProductosCobrados()
    {
        var producto = Mundo.CrearProducto(750m);
        var cobro = await S.Ventas.RegistrarParaLlevarAsync(new ParaLlevarRequest
        {
            ClaveIdempotencia = Guid.NewGuid(), IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = 1695m,
            Lineas = [new LineaPedidoRequest { IdProducto = producto.IdProducto, Cantidad = 2 }]
        }, Mundo.IdUsuario);
        Assert.True(cobro.Success, cobro.Message);
        using var consulta = Mundo.NuevaSesion();
        var detalle = await consulta.Ventas.ObtenerDetalleAsync(cobro.Data!.IdVenta!.Value);
        Assert.True(detalle.Success, detalle.Message);
        Assert.Equal(VentaTipos.Takeout, detalle.Data!.TipoVenta);
        Assert.Equal(producto.Nombre, Assert.Single(detalle.Data.Lineas).Producto);
        Assert.Equal(detalle.Data.Subtotal, detalle.Data.Lineas.Sum(l => l.Subtotal));
        Assert.Equal(1695m, detalle.Data.Total);
    }
}
