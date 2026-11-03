using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services;
using MonteCarlo.API.Services.Interfaces;
using MonteCarlo.Tests.Builders;
using MonteCarlo.Tests.Infra;
using NSubstitute;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Reservas;

/// <summary>HU-CON-002, criterio 8: una reserva con cuenta abierta no se cancela, no pasa a No Show ni se libera.</summary>
public class BloqueoConCuentaAbiertaTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private async Task<Reserva> ReservaConCuentaAbiertaAsync(bool conProductos = true)
    {
        var (reserva, _) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto();
        var abierta = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        if (!conProductos)
            await S.Ventas.AnularLineaAsync(abierta.Data!.Pedidos[0].Lineas[0].IdDetalle, new AnularLineaRequest(), Mundo.IdUsuario);
        return Mundo.ReservaActual(reserva.IdReserva);
    }

    private void AssertSigueConfirmada(Reserva reserva) =>
        Assert.Equal("Confirmada", Mundo.ReservaActual(reserva.IdReserva).EstadoReserva.Nombre);

    [SqlTheory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("HU", "CON-002"), Trait("Escenario", "8")]
    public async Task Cancelar_conCuentaAbierta_devuelve409YLaReservaSigueConfirmada(bool conProductos)
    {
        // conProductos = false: decision de diseno, la cuenta abierta vacia tambien bloquea (mas estricto que el texto original).
        var reserva = await ReservaConCuentaAbiertaAsync(conProductos);

        var r = await S.Reservas.CancelarAsync(reserva.CodigoReserva!);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("cuenta abierta", r.Message);
        AssertSigueConfirmada(reserva);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "8")]
    public async Task RegistrarNoShow_conCuentaAbierta_devuelve409()
    {
        var reserva = await ReservaConCuentaAbiertaAsync();

        var r = await S.Reservas.RegistrarNoShowAsync(reserva.CodigoReserva!);

        Assert.Equal(409, r.StatusCode);
        AssertSigueConfirmada(reserva);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "8")]
    public async Task CambiarEstadoACancelada_conCuentaAbierta_devuelve409()
    {
        var reserva = await ReservaConCuentaAbiertaAsync();

        var r = await S.Reservas.CambiarEstadoAsync(reserva.CodigoReserva!, "Cancelada");

        Assert.Equal(409, r.StatusCode);
        AssertSigueConfirmada(reserva);
    }




}
