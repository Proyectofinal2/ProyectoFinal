using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Reservas;

/// <summary>Reservas del dia informa el consumo de cada reserva (HU-CON-002 esc. 12).</summary>
public class ReservasDelDiaConsumoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Now);

    private async Task<ReservaDelDiaResponse> DelDiaAsync(int idReserva) =>
        (await S.Reservas.ObtenerDelDiaAsync(Hoy)).Data!.Single(r => r.IdReserva == idReserva);

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "12")]
    public async Task ObtenerDelDia_cuentaAbiertaConProductos_informaTotalFolioYProductos()
    {
        var reserva = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy, hora: new TimeOnly(0, 0), fechaAtencion: DateTime.Now.AddMinutes(-5));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 3)), Mundo.IdUsuario);
        var cuenta = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!;

        var consumo = (await DelDiaAsync(reserva.IdReserva)).Consumo!;

        Assert.Equal("Abierta", consumo.Estado);
        Assert.Equal(cuenta.Total, consumo.Total);
        Assert.Equal(3, consumo.Productos);
        Assert.Equal(cuenta.Folio, consumo.Folio);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "12")]
    public async Task ObtenerDelDia_cuentaCobrada_informaLaVentaCobrada()
    {
        var reserva = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy, hora: new TimeOnly(0, 0), fechaAtencion: DateTime.Now.AddMinutes(-5));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        var abierta = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!;
        await S.Ventas.CobrarAsync(reserva.IdReserva,
            new CobrarRequest { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = abierta.Total, Version = abierta.Version! },
            Mundo.IdUsuario);

        var consumo = (await DelDiaAsync(reserva.IdReserva)).Consumo!;

        Assert.Equal("Cobrada", consumo.Estado);
        Assert.Equal(abierta.Total, consumo.Total);
    }

    [SqlFact]
    [Trait("HU", "OPE-003")]
    public async Task ObtenerDelDia_sinLlegadaYHoraPasada_informaMinutosDeRetraso()
    {
        var reserva = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy.AddDays(-1), hora: new TimeOnly(12, 0), atendida: false);
        var futura = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy.AddDays(1), hora: new TimeOnly(12, 0), atendida: false);

        var delDia = (await S.Reservas.ObtenerDelDiaAsync(Hoy.AddDays(-1), Hoy.AddDays(1))).Data!;

        Assert.True(delDia.Single(r => r.IdReserva == reserva.IdReserva).MinutosDeRetraso >= 12 * 60);
        Assert.Null(delDia.Single(r => r.IdReserva == futura.IdReserva).MinutosDeRetraso);
    }

    [SqlFact]
    [Trait("HU", "OPE-003")]
    public async Task ObtenerDelDia_clienteQuePasoSuTiempo_informaMinutosExcedidos()
    {
        var excedida = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy, hora: new TimeOnly(0, 0), fechaAtencion: DateTime.Now.AddHours(-5));
        var enTiempo = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy, hora: new TimeOnly(0, 0), fechaAtencion: DateTime.Now.AddMinutes(-5));

        var delDia = (await S.Reservas.ObtenerDelDiaAsync(Hoy)).Data!;

        var enExceso = delDia.Single(r => r.IdReserva == excedida.IdReserva);
        Assert.True(enExceso.MinutosExcedidos > 0);
        Assert.Null(enExceso.MinutosDeRetraso);
        Assert.Null(delDia.Single(r => r.IdReserva == enTiempo.IdReserva).MinutosExcedidos);
    }

    [SqlFact]
    [Trait("HU", "OPE-003")]
    public async Task RegistrarAtencion_reservaDeOtroDia_seRechaza()
    {
        var manana = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy.AddDays(1), hora: new TimeOnly(12, 0), atendida: false);
        var hoy = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy, hora: new TimeOnly(12, 0), atendida: false);

        var deManana = await S.Reservas.RegistrarAtencionAsync(manana.CodigoReserva!);
        var deHoy = await S.Reservas.RegistrarAtencionAsync(hoy.CodigoReserva!);

        Assert.False(deManana.Success);
        Assert.Contains("mismo dia", deManana.Message);
        Assert.True(deHoy.Success, deHoy.Message);
    }

    [SqlFact]
    [Trait("HU", "OPE-005")]
    public async Task ObtenerDelDia_conRango_incluyeAmbosExtremosYExcluyeLoDemas()
    {
        var mesa = Mundo.CrearMesa();
        var ayer = Mundo.CrearReserva([mesa], fecha: Hoy.AddDays(-1), hora: new TimeOnly(12, 0));
        var hoy = Mundo.CrearReserva([mesa], fecha: Hoy, hora: new TimeOnly(12, 0));
        var manana = Mundo.CrearReserva([mesa], fecha: Hoy.AddDays(1), hora: new TimeOnly(12, 0));

        var ids = (await S.Reservas.ObtenerDelDiaAsync(Hoy.AddDays(-1), Hoy)).Data!.Select(r => r.IdReserva).ToList();

        Assert.Contains(ayer.IdReserva, ids);
        Assert.Contains(hoy.IdReserva, ids);
        Assert.DoesNotContain(manana.IdReserva, ids);
    }

    [SqlFact]
    [Trait("HU", "OPE-005")]
    public async Task ObtenerDelDia_rangoInvertidoOMuyLargo_esInvalido()
    {
        Assert.False((await S.Reservas.ObtenerDelDiaAsync(Hoy, Hoy.AddDays(-1))).Success);
        Assert.False((await S.Reservas.ObtenerDelDiaAsync(Hoy, Hoy.AddDays(92))).Success);
        Assert.True((await S.Reservas.ObtenerDelDiaAsync(Hoy, Hoy.AddDays(91))).Success);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "12")]
    public async Task ObtenerDelDia_reservaSinCuenta_noTieneConsumo()
    {
        var reserva = Mundo.CrearReserva([Mundo.CrearMesa()], fecha: Hoy, hora: new TimeOnly(0, 0), fechaAtencion: DateTime.Now.AddMinutes(-5));

        Assert.Null((await DelDiaAsync(reserva.IdReserva)).Consumo);
    }
}
