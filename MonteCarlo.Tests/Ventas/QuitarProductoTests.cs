using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

/// <summary>HU-CON-002, criterio 6: corregir la cuenta abierta (quitar no borra, anula).</summary>
public class QuitarProductoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private async Task<(int IdReserva, CuentaResponse Cuenta, Producto Producto)> CuentaAbiertaAsync(int cantidad = 3, decimal precio = 1000m)
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(precio);
        var r = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, cantidad)), Mundo.IdUsuario);
        return (reserva.IdReserva, r.Data!, producto);
    }

    private static int IdLinea(CuentaResponse cuenta) => cuenta.Pedidos.SelectMany(p => p.Lineas).First(l => !l.Anulado).IdDetalle;

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "6")]
    public async Task Anular_lineaCompleta_quedaAnuladaConQuienYCuandoYElTotalSeRecalcula()
    {
        var (idReserva, cuenta, _) = await CuentaAbiertaAsync(cantidad: 2, precio: 1500m);
        var otro = Mundo.CrearProducto(500m);
        await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((otro.IdProducto, 1)), Mundo.IdUsuario);

        var antes = DateTime.Now.AddSeconds(-2);
        var r = await S.Ventas.AnularLineaAsync(IdLinea(cuenta), new AnularLineaRequest(), Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Equal(500m, r.Data!.Total);
        var lineas = r.Data.Pedidos.SelectMany(p => p.Lineas).ToList();
        Assert.Equal(2, lineas.Count);                       // la anulada sigue en el detalle
        Assert.Single(lineas, l => l.Anulado);
        var linea = Mundo.VentaDe(idReserva)!.Detalles.Single(d => d.Anulado);
        Assert.Equal(Mundo.IdUsuario, linea.IdUsuarioAnulacion);
        Assert.True(linea.FechaAnulacion >= antes);
        Assert.Equal("Quitado de la cuenta", linea.MotivoAnulacion);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "6")]
    public async Task Anular_quitarUnoDeTres_anulaLaOriginalYCreaReemplazoConDosEnElMismoPedido()
    {
        var (idReserva, cuenta, _) = await CuentaAbiertaAsync(cantidad: 3, precio: 1000m);
        var pedido = cuenta.Pedidos.Single();

        var r = await S.Ventas.AnularLineaAsync(IdLinea(cuenta), new AnularLineaRequest { Cantidad = 1 }, Mundo.IdUsuario);

        Assert.Equal(2000m, r.Data!.Total);
        Assert.Equal(2, r.Data.TotalProductos);
        var venta = Mundo.VentaDe(idReserva)!;
        Assert.Equal(2, venta.Detalles.Count);
        var original = venta.Detalles.Single(d => d.Anulado);
        var reemplazo = venta.Detalles.Single(d => !d.Anulado);
        Assert.Equal(3, original.Cantidad);
        Assert.Equal(2, reemplazo.Cantidad);
        Assert.Equal(original.IdPedido, reemplazo.IdPedido);
        Assert.Equal(pedido.IdPedido, reemplazo.IdPedido);
        Assert.Equal(original.PrecioUnitario, reemplazo.PrecioUnitario);
    }





    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "6")]
    public async Task Anular_enCuentaCobrada_devuelve409YNoModificaLaLinea()
    {
        var (idReserva, cuenta, _) = await CuentaAbiertaAsync();
        await S.Ventas.CobrarAsync(idReserva,
            new CobrarRequest { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = cuenta.Total, Version = cuenta.Version },
            Mundo.IdUsuario);

        var r = await S.Ventas.AnularLineaAsync(IdLinea(cuenta), new AnularLineaRequest(), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.DoesNotContain(Mundo.VentaDe(idReserva)!.Detalles, d => d.Anulado);
    }




    internal static List<string> Validar(object modelo)
    {
        var resultados = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            modelo, new System.ComponentModel.DataAnnotations.ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados.Select(r => r.ErrorMessage ?? "").ToList();
    }
}
