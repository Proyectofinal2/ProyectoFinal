using Microsoft.Extensions.Logging;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services;
using MonteCarlo.API.Services.Interfaces;
using NSubstitute;

namespace MonteCarlo.Tests.Ventas;

public class IvaParaLlevarTests
{
    private readonly IVentasRepository ventas = Substitute.For<IVentasRepository>();
    private readonly IProductosRepository productos = Substitute.For<IProductosRepository>();
    private readonly IMetodosPagoRepository metodos = Substitute.For<IMetodosPagoRepository>();
    private readonly IConfiguracionService configuracion = Substitute.For<IConfiguracionService>();

    private VentasService Preparar(decimal precio, decimal tasa)
    {
        productos.ObtenerDisponiblesParaVentaAsync(Arg.Any<IReadOnlyCollection<int>>())
            .Returns(new List<Producto> { new() { IdProducto = 1, Precio = precio } });
        metodos.ObtenerPorIdAsync(1).Returns(new MetodoPago { IdMetodoPago = 1, Nombre = "Efectivo", Activo = true });
        configuracion.ObtenerTasaIvaAsync().Returns(Result<decimal>.Ok(tasa));
        return new VentasService(ventas, Substitute.For<IReservasRepository>(), productos, metodos, configuracion, Substitute.For<ILogger<VentasService>>());
    }

    private static ParaLlevarRequest Solicitud(decimal total, int cantidad = 1) => new()
    {
        ClaveIdempotencia = Guid.NewGuid(), IdMetodoPago = 1, TotalEsperado = total,
        Lineas = [new LineaPedidoRequest { IdProducto = 1, Cantidad = cantidad }]
    };

    [Theory]
    [InlineData(13, 226)]
    [InlineData(0, 200)]
    [InlineData(20, 240)]
    public async Task VentaNuevaGuardaTasaEImportes(decimal tasa, decimal total)
    {
        var servicio = Preparar(100m, tasa);
        Venta? guardada = null;
        ventas.When(x => x.Agregar(Arg.Any<Venta>())).Do(x => guardada = x.Arg<Venta>());
        var resultado = await servicio.RegistrarParaLlevarAsync(Solicitud(total, 2), 1);
        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal(200m, guardada!.Subtotal);
        Assert.Equal(tasa, guardada.TasaIvaAplicada);
        Assert.Equal(total - 200m, guardada.ImporteIva);
        Assert.Equal(total, guardada.Total);
        Assert.Equal(total, resultado.Data!.Total);
    }

    [Fact]
    public async Task RedondeaYRechazaTotalDesactualizado()
    {
        var servicio = Preparar(0.50m, 1m);
        var conflicto = await servicio.RegistrarParaLlevarAsync(Solicitud(0.50m), 1);
        Assert.Equal(409, conflicto.StatusCode);
        var correcto = await servicio.RegistrarParaLlevarAsync(Solicitud(0.51m), 1);
        Assert.True(correcto.Success, correcto.Message);
        Assert.Equal(0.01m, correcto.Data!.ImporteIva);
    }

    [Fact]
    public async Task ConfiguracionInvalidaImpideVenta()
    {
        var servicio = Preparar(100m, 13m);
        configuracion.ObtenerTasaIvaAsync().Returns(Result<decimal>.BadRequest("Tasa inválida"));
        var resultado = await servicio.RegistrarParaLlevarAsync(Solicitud(113m), 1);
        Assert.False(resultado.Success);
        ventas.DidNotReceive().Agregar(Arg.Any<Venta>());
    }

    [Fact]
    public async Task ImporteFueraDePrecisionSeRechaza()
    {
        var servicio = Preparar(99999999.99m, 13m);
        var resultado = await servicio.RegistrarParaLlevarAsync(Solicitud(0m), 1);
        Assert.Equal(400, resultado.StatusCode);
        ventas.DidNotReceive().Agregar(Arg.Any<Venta>());
    }
}
