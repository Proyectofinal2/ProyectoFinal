using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Ventas;

/// <summary>
/// HU-CON-002, criterio 15 (cerrar sin cobro): una cuenta antigua con productos se cierra como Abandonada, con motivo, sin
/// registrar ingreso. Es antigua pasado el tiempo de uso de la mesa (90 min) mas la tolerancia (30): a las 2 h.
/// </summary>
public class CerrarSinCobroTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private async Task<(int IdReserva, int IdMesa, CuentaResponse Cuenta)> CuentaConProductosAsync(DateTime sentadoEn, int unidades = 2)
    {
        var mesa = Mundo.CrearMesa();
        var reserva = Mundo.CrearReserva([mesa], fecha: DateOnly.FromDateTime(sentadoEn), hora: new TimeOnly(0, 0), fechaAtencion: sentadoEn);
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, unidades)), Mundo.IdUsuario);
        var cuenta = (await S.Ventas.ObtenerCuentaAsync(reserva.IdReserva)).Data!;
        return (reserva.IdReserva, mesa.IdMesa, cuenta);
    }

    private static CerrarSinCobroRequest Cierre(CuentaResponse cuenta, string? motivo = "El cliente se fue sin pagar") =>
        new() { Motivo = motivo, Version = cuenta.Version };

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "15")]
    public async Task CerrarSinCobro_cuentaAntiguaConProductos_quedaAbandonadaConSuHistorial()
    {
        var (idReserva, idMesa, cuenta) = await CuentaConProductosAsync(DateTime.Now.AddMinutes(-130));

        var r = await S.Ventas.CerrarSinCobroAsync(idReserva, Cierre(cuenta), Mundo.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Equal(cuenta.Total, r.Data!.Total);
        Assert.Null(r.Data.MetodoPago);
        var venta = Mundo.VentaDe(idReserva)!;
        Assert.Equal(VentaEstados.Abandonada, venta.Estado);
        Assert.Equal(cuenta.Total, venta.Total);
        Assert.Equal("El cliente se fue sin pagar", venta.MotivoAnulacion);
        Assert.NotNull(venta.FechaAnulacion);
        Assert.Equal(Mundo.IdUsuario, venta.IdUsuarioAnulacion);
        Assert.Null(venta.IdMetodoPago);
        Assert.Null(venta.FechaCobro);
        var guardada = Mundo.ReservaActual(idReserva);
        Assert.Equal("Finalizada", guardada.EstadoReserva.Nombre);
        Assert.Equal("Cuenta cerrada sin cobro", guardada.MotivoCancelacion);
        Assert.Equal("Disponible", (await S.Mesas.ListarAsync(false)).Data!.Single(m => m.IdMesa == idMesa).Estado);

        var abandonada = (await S.Ventas.ObtenerCuentaAsync(idReserva)).Data!;
        Assert.Equal(VentaEstados.Abandonada, abandonada.Estado);
        Assert.Equal(2, abandonada.TotalProductos);
        Assert.False(abandonada.PuedeAgregar);
        Assert.Contains("cerrada sin cobro", abandonada.Bloqueo);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "15")]
    public async Task CerrarSinCobro_cuentaAunNoVencida_devuelve409YNoCambiaNada()
    {
        var (idReserva, _, cuenta) = await CuentaConProductosAsync(DateTime.Now.AddMinutes(-5));
        Assert.False(cuenta.PuedeCerrarSinCobro);

        var r = await S.Ventas.CerrarSinCobroAsync(idReserva, Cierre(cuenta), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("todavia no esta vencida", r.Message);
        Assert.Equal(VentaEstados.Abierta, Mundo.VentaDe(idReserva)!.Estado);
        Assert.Equal("Confirmada", Mundo.ReservaActual(idReserva).EstadoReserva.Nombre);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "15")]
    public async Task CerrarSinCobro_versionDesactualizada_devuelve409ConLaCuentaActual()
    {
        var (idReserva, _, cuenta) = await CuentaConProductosAsync(DateTime.Now.AddMinutes(-130));
        var otroProducto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(idReserva, Pedido((otroProducto.IdProducto, 1)), Mundo.IdUsuario);

        var r = await S.Ventas.CerrarSinCobroAsync(idReserva, Cierre(cuenta), Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.IsType<CuentaResponse>(r.ErrorData);
        Assert.Equal(VentaEstados.Abierta, Mundo.VentaDe(idReserva)!.Estado);
    }

    [SqlTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ab ")]
    [Trait("HU", "CON-002"), Trait("Escenario", "15")]
    public async Task CerrarSinCobro_motivoVacioOMuyCorto_devuelve400(string? motivo)
    {
        var (idReserva, _, cuenta) = await CuentaConProductosAsync(DateTime.Now.AddMinutes(-130));

        var r = await S.Ventas.CerrarSinCobroAsync(idReserva, Cierre(cuenta, motivo), Mundo.IdUsuario);

        Assert.Equal(400, r.StatusCode);
        Assert.Equal(VentaEstados.Abierta, Mundo.VentaDe(idReserva)!.Estado);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "15")]
    public async Task CerrarSinCobro_cuentaSinProductos_devuelve409QueMandaALiberar()
    {
        var (reserva, _) = Mundo.CrearReservaSentada();

        var r = await S.Ventas.CerrarSinCobroAsync(reserva.IdReserva, new CerrarSinCobroRequest { Motivo = "Sin consumo", Version = "AAAA" }, Mundo.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Null(Mundo.VentaDe(reserva.IdReserva));
        Assert.Equal("Confirmada", Mundo.ReservaActual(reserva.IdReserva).EstadoReserva.Nombre);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "15")]
    public async Task CerrarSinCobro_cuentaAntigua_seOfreceSoloCuandoVence()
    {
        var (_, _, vencida) = await CuentaConProductosAsync(DateTime.Now.AddMinutes(-130));
        var (_, _, reciente) = await CuentaConProductosAsync(DateTime.Now.AddMinutes(-5));

        Assert.True(vencida.PuedeCerrarSinCobro);
        Assert.False(reciente.PuedeCerrarSinCobro);
    }
}
