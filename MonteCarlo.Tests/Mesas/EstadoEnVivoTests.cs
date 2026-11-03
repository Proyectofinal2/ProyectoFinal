using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Builders;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Mesas;

/// <summary>
/// HU-CON-002, criterios 7 y 11: el listado en vivo de las mesas (MesasService.MapearEstadoEnVivo / EstaOcupandoAhora).
/// Las pruebas con horas relativas a "ahora" pueden fallar si se ejecutan en los 10 minutos previos a la medianoche.
/// </summary>
public class EstadoEnVivoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private async Task<MesaResponse> VivaAsync(int idMesa, bool incluirDeshabilitadas = false) =>
        (await S.Mesas.ListarAsync(incluirDeshabilitadas)).Data!.Single(m => m.IdMesa == idMesa);



    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "11")]
    public async Task Mesa_conReservaConfirmadaCuyaHoraYaLlegoSinLlegadaRegistrada_estaOcupadaEsperandoLlegada()
    {
        var mesa = Mundo.CrearMesa();
        var reserva = Mundo.CrearReserva([mesa], hora: Mundo.HoraReciente(10), atendida: false);

        var viva = await VivaAsync(mesa.IdMesa);

        Assert.Equal("Ocupada", viva.Estado);
        Assert.True(viva.EsperandoLlegada);
        Assert.Null(viva.Cuenta);
        Assert.Equal(reserva.IdReserva, viva.ReservaActual!.IdReserva);
        Assert.Null(viva.ReservaActual.FechaAtencion);
        // Esperando: el tiempo corre desde la hora de la reserva.
        Assert.InRange(viva.ReservaActual.MinutosSentados, 9, 11);
    }


    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "7")]
    public async Task Mesa_ocupadaSinPedido_noTieneCuentaYContabilizaMinutosDesdeLaLlegada()
    {
        var (reserva, mesa) = Mundo.CrearReservaSentada();   // llegada hace 5 minutos

        var viva = await VivaAsync(mesa.IdMesa);

        Assert.Equal("Ocupada", viva.Estado);
        Assert.False(viva.EsperandoLlegada);
        Assert.Null(viva.Cuenta);
        Assert.Equal(2, viva.PersonasActuales);
        Assert.Equal(reserva.IdReserva, viva.ReservaActual!.IdReserva);
        Assert.InRange(viva.ReservaActual.MinutosSentados, 4, 6);
        Assert.Equal(viva.ReservaActual.OcupadaDesde, viva.ReservaActual.FechaAtencion);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "7")]
    public async Task Mesa_conCuentaAbierta_muestraTotalProductosYVersion_yDejaDeMostrarlaAlCobrar()
    {
        var (reserva, mesa) = Mundo.CrearReservaSentada();
        var producto = Mundo.CrearProducto(1200m);
        var abierta = await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 3)), Mundo.IdUsuario);

        var viva = await VivaAsync(mesa.IdMesa);

        Assert.Equal(3600m, viva.Cuenta!.Total);
        Assert.Equal(3, viva.Cuenta.Productos);
        Assert.Equal(abierta.Data!.Version, viva.Cuenta.Version);

        await S.Ventas.CobrarAsync(reserva.IdReserva,
            new CobrarRequest { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = 3600m, Version = abierta.Data.Version }, Mundo.IdUsuario);
        var libre = await VivaAsync(mesa.IdMesa);
        Assert.Equal("Disponible", libre.Estado);
        Assert.Null(libre.Cuenta);
    }


    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "7")]
    public async Task Mesa_conCuentaAbiertaDeUnDiaAnterior_siguenOcupada()
    {
        var mesa = Mundo.CrearMesa();
        var ayer = Mundo.CrearReserva([mesa], fecha: Mundo.Hoy.AddDays(-1), hora: new TimeOnly(20, 0));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(ayer.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);

        var viva = await VivaAsync(mesa.IdMesa);

        Assert.Equal("Ocupada", viva.Estado);
        Assert.NotNull(viva.Cuenta);
        Assert.Equal(ayer.IdReserva, viva.ReservaActual!.IdReserva);
    }



}
