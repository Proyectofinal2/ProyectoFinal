using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Mesas;

/// <summary>
/// La cuenta abierta antigua se marca en Mesas y en la cuenta (HU-CON-002 esc. 13).
/// Es antigua (por cobrar) pasado el tiempo de uso de la mesa (DuracionReservaMinutos = 90 en la base de pruebas) mas la
/// tolerancia (ToleranciaCobroMinutos = 30): a las 2 h de haberse sentado.
/// </summary>
public class CuentaAntiguaTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private async Task<(int IdReserva, int IdMesa)> CuentaAbiertaAsync(DateTime sentadoEn)
    {
        var mesa = Mundo.CrearMesa();
        var reserva = Mundo.CrearReserva([mesa], fecha: DateOnly.FromDateTime(sentadoEn), hora: new TimeOnly(0, 0),
            fechaAtencion: sentadoEn);
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        return (reserva.IdReserva, mesa.IdMesa);
    }

    private async Task<MesaResponse> MesaAsync(int idMesa) =>
        (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == idMesa);

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task Mesas_cuentaAbiertaDeAyer_sePasaAAntiguaConLaFechaDeVencimiento()
    {
        var (_, idMesa) = await CuentaAbiertaAsync(DateTime.Now.AddDays(-1));

        var mesa = await MesaAsync(idMesa);

        Assert.True(mesa.Cuenta!.Antigua);
        Assert.True(mesa.Cuenta.VenceDesde < DateTime.Now);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task Mesas_cuentaAbiertaDeHoy_noEsAntigua()
    {
        var (_, idMesa) = await CuentaAbiertaAsync(DateTime.Now.AddMinutes(-5));

        var mesa = await MesaAsync(idMesa);

        Assert.False(mesa.Cuenta!.Antigua);
        Assert.True(mesa.Cuenta.VenceDesde > DateTime.Now);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task Mesas_pasadoElTurnoPeroDentroDelMargen_todaviaNoEsAntigua()
    {
        var (_, idMesa) = await CuentaAbiertaAsync(DateTime.Now.AddMinutes(-100));   // turno de 90 + margen de 30 = 120

        Assert.False((await MesaAsync(idMesa)).Cuenta!.Antigua);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task Mesas_conToleranciaCero_esAntiguaJustoAlTerminarElTurno()
    {
        await using var db = Mundo.NuevoContexto();
        var original = db.ConfiguracionesSistema.Single(c => c.Clave == "ToleranciaCobroMinutos").Valor;
        await db.ConfiguracionesSistema.Where(c => c.Clave == "ToleranciaCobroMinutos")
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.Valor, "0"));
        try
        {
            var (_, idMesa) = await CuentaAbiertaAsync(DateTime.Now.AddMinutes(-100));   // turno de 90, tolerancia 0

            Assert.True((await MesaAsync(idMesa)).Cuenta!.Antigua);
        }
        finally
        {
            await db.ConfiguracionesSistema.Where(c => c.Clave == "ToleranciaCobroMinutos")
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.Valor, original));
        }
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task Mesas_pasadoElTurnoYLaTolerancia_esAntigua()
    {
        var (_, idMesa) = await CuentaAbiertaAsync(DateTime.Now.AddMinutes(-130));

        var mesa = await MesaAsync(idMesa);

        Assert.True(mesa.Cuenta!.Antigua);
        Assert.True(mesa.Cuenta.VenceDesde < DateTime.Now);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task Resumen_cuentaAntigua_seCuentaEnCuentasAntiguas()
    {
        await CuentaAbiertaAsync(DateTime.Now.AddDays(-1));

        var resumen = (await S.Mesas.ObtenerResumenAsync()).Data!;

        Assert.True(resumen.CuentasAntiguas >= 1);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "7")]
    public async Task Resumen_cuentasAbiertas_sumaElConsumoUnaVezPorReservaAunqueSeanDosMesas()
    {
        var antes = (await S.Mesas.ObtenerResumenAsync()).Data!;
        var (idSimple, _) = await CuentaAbiertaAsync(DateTime.Now.AddMinutes(-5));
        var mesaA = Mundo.CrearMesa();
        var mesaB = Mundo.CrearMesa();
        var unida = Mundo.CrearReserva([mesaA, mesaB], fecha: DateOnly.FromDateTime(DateTime.Now), hora: new TimeOnly(0, 0),
            fechaAtencion: DateTime.Now.AddMinutes(-5));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(unida.IdReserva, Pedido((producto.IdProducto, 2)), Mundo.IdUsuario);
        var consumo = (await S.Ventas.ObtenerCuentaAsync(idSimple)).Data!.Total
            + (await S.Ventas.ObtenerCuentaAsync(unida.IdReserva)).Data!.Total;

        var despues = (await S.Mesas.ObtenerResumenAsync()).Data!;

        Assert.Equal(2, despues.CuentasAbiertas - antes.CuentasAbiertas);
        Assert.Equal(consumo, despues.ConsumoEnCurso - antes.ConsumoEnCurso);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task ObtenerCuenta_abiertaDeAyer_esAntiguaYLaDeHoyNo()
    {
        var (ayer, _) = await CuentaAbiertaAsync(DateTime.Now.AddDays(-1));
        var (hoy, _) = await CuentaAbiertaAsync(DateTime.Now.AddMinutes(-5));

        var cuentaAyer = (await S.Ventas.ObtenerCuentaAsync(ayer)).Data!;
        var cuentaHoy = (await S.Ventas.ObtenerCuentaAsync(hoy)).Data!;

        Assert.True(cuentaAyer.Antigua);
        Assert.NotNull(cuentaAyer.VenceDesde);
        Assert.False(cuentaHoy.Antigua);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public async Task ObtenerCuenta_cobrada_noSeMarcaAntigua()
    {
        var (idReserva, _) = await CuentaAbiertaAsync(DateTime.Now.AddDays(-1));
        var abierta = (await S.Ventas.ObtenerCuentaAsync(idReserva)).Data!;
        await S.Ventas.CobrarAsync(idReserva,
            new CobrarRequest { IdMetodoPago = Mundo.IdMetodoPago, TotalEsperado = abierta.Total, Version = abierta.Version! },
            Mundo.IdUsuario);

        var cuenta = (await S.Ventas.ObtenerCuentaAsync(idReserva)).Data!;

        Assert.False(cuenta.Antigua);
        Assert.Null(cuenta.VenceDesde);
    }
}
