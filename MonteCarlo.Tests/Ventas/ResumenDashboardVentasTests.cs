using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Ventas;

/// <summary>Indicadores historicos: solo ventas Cobradas por FechaCobro y con desempate determinista por metodo.</summary>
public class ResumenDashboardVentasTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private int CrearMetodo(string nombre, bool activo = true)
    {
        using var db = Mundo.NuevoContexto();
        var metodo = new MetodoPago { Nombre = nombre, Activo = activo };
        db.MetodosPago.Add(metodo);
        db.SaveChanges();
        return metodo.IdMetodoPago;
    }

    private void CrearVenta(string estado, decimal total, DateTime fecha, int? idMetodo = null, int? idReserva = null,
        decimal? subtotal = null, decimal? tasa = null, decimal? iva = null)
    {
        using var db = Mundo.NuevoContexto();
        var venta = new Venta
        {
            TipoVenta = idReserva is null ? VentaTipos.Takeout : VentaTipos.Consumo,
            Estado = estado,
            IdReserva = idReserva,
            IdMetodoPago = idMetodo,
            Total = total,
            Subtotal = subtotal,
            TasaIvaAplicada = tasa,
            ImporteIva = iva,
            IdUsuarioRegistro = Mundo.IdUsuario,
            FechaVenta = fecha.AddMinutes(-5)
        };

        if (estado is VentaEstados.Cobrada or VentaEstados.Anulada)
        {
            venta.FechaCobro = fecha;
            venta.IdUsuarioCobro = Mundo.IdUsuario;
        }

        if (estado is VentaEstados.Anulada or VentaEstados.Abandonada)
        {
            venta.MotivoAnulacion = "Prueba de indicador";
            venta.FechaAnulacion = fecha;
            venta.IdUsuarioAnulacion = Mundo.IdUsuario;
        }

        db.Ventas.Add(venta);
        db.SaveChanges();
    }

    [SqlFact]
    public async Task ListadoYDashboard_sumanTotalBrutoDeMesaYVentaHistorica()
    {
        var fecha = new DateTime(2040, 2, 1, 14, 0, 0);
        var dia = DateOnly.FromDateTime(fecha);
        var (mesa, _) = Mundo.CrearReservaSentada();
        var metodo = CrearMetodo("Mesa con IVA");
        CrearVenta(VentaEstados.Cobrada, 113.28m, fecha, metodo, mesa.IdReserva, 100.25m, 13m, 13.03m);
        CrearVenta(VentaEstados.Cobrada, 110.50m, fecha, Mundo.IdMetodoPago);
        var (abierta, _) = Mundo.CrearReservaSentada();
        CrearVenta(VentaEstados.Abierta, 500m, fecha, idReserva: abierta.IdReserva);
        var listado = await S.Ventas.ObtenerListadoAsync(new FiltroVentasRequest
        { FechaInicio = dia, FechaFin = dia, Estado = VentaEstados.Cobrada });
        Assert.True(listado.Success, listado.Message);
        Assert.Equal(2, listado.Data!.Count);
        Assert.Equal(113.28m, listado.Data.Single(v => v.TipoVenta == VentaTipos.Consumo).Total);
        Assert.Equal(110.50m, listado.Data.Single(v => v.TipoVenta == VentaTipos.Takeout).Total);
        var resumen = await S.Ventas.ObtenerResumenDashboardAsync(fecha, fecha.AddMinutes(1));
        Assert.True(resumen.Success, resumen.Message);
        Assert.Equal(223.78m, resumen.Data!.IngresosCobrados);
        Assert.Equal(2, resumen.Data.VentasCobradas);
        Assert.Equal(111.89m, resumen.Data.TicketPromedio);
        Assert.Equal("Mesa con IVA", resumen.Data.MetodoPagoPredominante);
        Assert.Equal(50.6m, resumen.Data.PorcentajeMetodoPagoPredominante);
    }

    [SqlFact]
    public async Task Resumen_incluyeSoloCobradasDentroDelIntervalo_yCalculaTicket()
    {
        var ahora = new DateTime(2040, 1, 2, 12, 0, 0);
        var inicio = ahora.Date;
        var fin = ahora.AddMinutes(1);
        var metodo = CrearMetodo($"Resumen {Guid.NewGuid():N}"[..20]);

        CrearVenta(VentaEstados.Cobrada, 1000m, ahora.AddSeconds(-2), metodo);
        CrearVenta(VentaEstados.Cobrada, 3000m, ahora.AddSeconds(-1), metodo);
        var (reservaAbierta, _) = Mundo.CrearReservaSentada();
        CrearVenta(VentaEstados.Abierta, 8000m, ahora, null, reservaAbierta.IdReserva);
        CrearVenta(VentaEstados.Anulada, 7000m, ahora, metodo);
        var (reserva, _) = Mundo.CrearReservaSentada();
        CrearVenta(VentaEstados.Abandonada, 6000m, ahora, null, reserva.IdReserva);
        CrearVenta(VentaEstados.Cobrada, 9000m, inicio.AddDays(-1).AddHours(12), metodo);

        var resultado = await S.Ventas.ObtenerResumenDashboardAsync(inicio, fin);

        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal(4000m, resultado.Data!.IngresosCobrados);
        Assert.Equal(2, resultado.Data.VentasCobradas);
        Assert.Equal(2000m, resultado.Data.TicketPromedio);
    }

    [SqlFact]
    public async Task Resumen_incluyeTakeout_yConservaMetodoHistoricoDeshabilitado()
    {
        var ahora = new DateTime(2040, 1, 5, 12, 0, 0);
        var nombre = $"Historico {Guid.NewGuid():N}"[..20];
        var metodo = CrearMetodo(nombre);
        CrearVenta(VentaEstados.Cobrada, 2500m, ahora, metodo);
        using (var db = Mundo.NuevoContexto())
        {
            db.MetodosPago.Single(m => m.IdMetodoPago == metodo).Activo = false;
            db.SaveChanges();
        }

        var resultado = await S.Ventas.ObtenerResumenDashboardAsync(ahora.Date, ahora.AddMinutes(1));

        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal(2500m, resultado.Data!.IngresosCobrados);
        Assert.Equal(nombre, resultado.Data.MetodoPagoPredominante);
        Assert.Equal(100m, resultado.Data.PorcentajeMetodoPagoPredominante);
    }

    [SqlFact]
    public async Task Resumen_desempataMetodoPorNombre_yCalculaParticipacionPorImporte()
    {
        var ahora = new DateTime(2040, 1, 8, 12, 0, 0);
        var metodoA = CrearMetodo("A Empate");
        var metodoZ = CrearMetodo("Z Empate");
        CrearVenta(VentaEstados.Cobrada, 3000m, ahora, metodoA);
        CrearVenta(VentaEstados.Cobrada, 3000m, ahora, metodoZ);
        CrearVenta(VentaEstados.Cobrada, 4000m, ahora, metodoZ);

        var resultado = await S.Ventas.ObtenerResumenDashboardAsync(ahora.Date, ahora.AddMinutes(1));

        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal("Z Empate", resultado.Data!.MetodoPagoPredominante);
        Assert.Equal(70m, resultado.Data.PorcentajeMetodoPagoPredominante);
    }

    [SqlFact]
    public async Task Resumen_enEmpateDeImporte_eligeElMetodoConNombreAlfabeticamenteMenor()
    {
        var ahora = new DateTime(2040, 1, 11, 12, 0, 0);
        var metodoZ = CrearMetodo("Z Empate Total");
        var metodoA = CrearMetodo("A Empate Total");
        CrearVenta(VentaEstados.Cobrada, 5000m, ahora, metodoZ);
        CrearVenta(VentaEstados.Cobrada, 5000m, ahora, metodoA);

        var resultado = await S.Ventas.ObtenerResumenDashboardAsync(ahora.Date, ahora.AddMinutes(1));

        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal("A Empate Total", resultado.Data!.MetodoPagoPredominante);
        Assert.Equal(50m, resultado.Data.PorcentajeMetodoPagoPredominante);
    }

    [SqlFact]
    public async Task Resumen_sinCobros_devuelveCeros_yMetodoNulo()
    {
        var ahora = new DateTime(2040, 1, 14, 12, 0, 0);

        var resultado = await S.Ventas.ObtenerResumenDashboardAsync(ahora.Date, ahora.AddMinutes(1));

        Assert.True(resultado.Success, resultado.Message);
        Assert.Equal(0m, resultado.Data!.IngresosCobrados);
        Assert.Equal(0, resultado.Data.VentasCobradas);
        Assert.Equal(0m, resultado.Data.TicketPromedio);
        Assert.Null(resultado.Data.MetodoPagoPredominante);
        Assert.Null(resultado.Data.PorcentajeMetodoPagoPredominante);
    }
}
