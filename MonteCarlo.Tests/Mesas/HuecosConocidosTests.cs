using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Builders;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Mesas;

/// <summary>
/// Pruebas que FIJAN el comportamiento actual de huecos conocidos (no son defectos nuevos ni se corrigen aqui).
/// Cuando se implemente el plan indicado, la asercion marcada con "HOY" debe invertirse.
/// </summary>
public class HuecosConocidosTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    /// <summary>Mesa de capacidad unica con una cuenta abierta de AYER (el turno ya paso, la mesa sigue Ocupada).</summary>
    private async Task<(MonteCarlo.API.Data.Entities.Mesa Mesa, MonteCarlo.API.Data.Entities.Reserva Ayer)> MesaConCuentaDeAyerAsync(int capacidad)
    {
        var mesa = Mundo.CrearMesa(capacidad);
        var ayer = Mundo.CrearReserva([mesa], fecha: Mundo.Hoy.AddDays(-1), hora: new TimeOnly(20, 0), personas: 2,
            fechaAtencion: DateTime.Now.AddDays(-1));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(ayer.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        var viva = (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == mesa.IdMesa);
        Assert.Equal("Ocupada", viva.Estado);   // precondicion: la cuenta abierta la mantiene ocupada
        return (mesa, ayer);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14"), Trait("Hueco", "G1")]
    public async Task G1_crearWalkInSobreMesaOcupadaPorCuentaAbierta_esConflicto()
    {
        var (mesa, _) = await MesaConCuentaDeAyerAsync(capacidad: 20);

        var r = await S.Reservas.CrearWalkInAsync(new CrearReservaWalkInRequest { CantidadPersonas = 2, IdsMesa = [mesa.IdMesa] });

        Assert.Equal(409, r.StatusCode);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14"), Trait("Hueco", "G1")]
    public async Task G1_sugerirWalkInNoOfreceLaMesaConCuentaAbierta()
    {
        var (mesa, _) = await MesaConCuentaDeAyerAsync(capacidad: 20);

        var r = await S.Reservas.SugerirWalkInAsync(2);

        Assert.DoesNotContain(mesa.NumeroMesa, r.Data!.NumerosMesa);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Hueco", "R1")]
    public async Task R1_liberarPorMesaEncuentraLaCuentaAbiertaVaciaDeAyer()
    {
        var mesa = Mundo.CrearMesa();
        var ayer = Mundo.CrearReserva([mesa], fecha: Mundo.Hoy.AddDays(-1), hora: new TimeOnly(20, 0), personas: 2,
            fechaAtencion: DateTime.Now.AddDays(-1));
        var producto = Mundo.CrearProducto();
        var abierta = await S.Ventas.AgregarPedidoAsync(ayer.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        await S.Ventas.AnularLineaAsync(abierta.Data!.Pedidos[0].Lineas[0].IdDetalle, new AnularLineaRequest(), Mundo.IdUsuario);

        var r = await S.Ventas.LiberarSinConsumoPorMesaAsync(mesa.IdMesa, Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
    }



    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Hueco", "CuentaVieja")]
    public async Task CuentaAbiertaDeHaceDosDias_HOY_mantieneLaMesaOcupadaSinLimite()
    {
        // TODO de MesasService.EstaOcupandoAhora: no hay vencimiento; el plan define Abandonada / cierre automatico.
        var mesa = Mundo.CrearMesa();
        var vieja = Mundo.CrearReserva([mesa], fecha: Mundo.Hoy.AddDays(-2), hora: new TimeOnly(20, 0),
            fechaAtencion: DateTime.Now.AddDays(-2));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(vieja.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        var viva = (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == mesa.IdMesa);

        Assert.Equal("Ocupada", viva.Estado);
        Assert.True(viva.ReservaActual!.MinutosSentados >= 2 * 24 * 60 - 5);   // HOY: sin tope de tiempo
    }
}
