using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Configuracion;

/// <summary>Auditoría de altas y cambios efectivos de métodos de pago.</summary>
public class HistorialMetodosPagoTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private async Task<List<HistorialMetodoPago>> HistorialDeAsync(int idMetodoPago)
    {
        await using var db = Mundo.NuevoContexto();
        return await db.HistorialMetodosPago.AsNoTracking()
            .Where(h => h.IdMetodoPago == idMetodoPago)
            .OrderBy(h => h.IdHistorialMetodoPago)
            .ToListAsync();
    }

    [SqlFact]
    [Trait("HU", "CFG-007"), Trait("Escenario", "Auditoría")]
    public async Task Crear_registraElMetodoYLaAuditoriaConElResponsable()
    {
        var responsable = Mundo.CrearUsuario("General");
        var nombre = $"Pago {Guid.NewGuid():N}"[..30];

        var resultado = await S.Configuracion.CrearMetodoPagoAsync(
            new CrearMetodoPagoRequest { Nombre = nombre }, responsable.IdUsuario);

        Assert.True(resultado.Success, resultado.Message);
        var fila = Assert.Single(await HistorialDeAsync(resultado.Data!.IdMetodoPago));
        Assert.Equal(HistorialMetodoPagoAcciones.Creado, fila.Accion);
        Assert.Equal(nombre, fila.NombreNuevo);
        Assert.True(fila.ActivoNuevo);
        Assert.Null(fila.NombreAnterior);
        Assert.Null(fila.ActivoAnterior);
        Assert.Equal(responsable.IdUsuario, fila.IdUsuario);
    }

    [SqlFact]
    [Trait("HU", "CFG-007"), Trait("Escenario", "Auditoría")]
    public async Task CambiarEstado_registraSoloCambiosEfectivos()
    {
        var responsable = Mundo.CrearUsuario("General");
        var nombre = $"Pago {Guid.NewGuid():N}"[..30];
        var creado = await S.Configuracion.CrearMetodoPagoAsync(
            new CrearMetodoPagoRequest { Nombre = nombre }, responsable.IdUsuario);
        var idMetodoPago = creado.Data!.IdMetodoPago;

        var sinCambio = await S.Configuracion.CambiarEstadoMetodoPagoAsync(idMetodoPago, activo: true, responsable.IdUsuario);
        var cambio = await S.Configuracion.CambiarEstadoMetodoPagoAsync(idMetodoPago, activo: false, responsable.IdUsuario);

        Assert.True(sinCambio.Success && cambio.Success);
        var historial = await HistorialDeAsync(idMetodoPago);
        Assert.Equal(2, historial.Count);
        var fila = historial.Single(h => h.Accion == HistorialMetodoPagoAcciones.Modificado);
        Assert.Equal(nombre, fila.NombreAnterior);
        Assert.Equal(nombre, fila.NombreNuevo);
        Assert.True(fila.ActivoAnterior);
        Assert.False(fila.ActivoNuevo);
        Assert.Equal(responsable.IdUsuario, fila.IdUsuario);
    }

    [SqlFact]
    [Trait("HU", "CFG-007"), Trait("Escenario", "Auditoría")]
    public async Task Crear_duplicadoNoRegistraAuditoria()
    {
        var responsable = Mundo.CrearUsuario("General");
        var existente = Mundo.IdMetodoPago;
        await using var db = Mundo.NuevoContexto();
        var nombreExistente = await db.MetodosPago.AsNoTracking()
            .Where(m => m.IdMetodoPago == existente).Select(m => m.Nombre).SingleAsync();

        var resultado = await S.Configuracion.CrearMetodoPagoAsync(
            new CrearMetodoPagoRequest { Nombre = nombreExistente }, responsable.IdUsuario);

        Assert.False(resultado.Success);
        Assert.Empty(await HistorialDeAsync(existente));
    }

    [SqlFact]
    [Trait("HU", "CFG-007"), Trait("Escenario", "Consulta")]
    public async Task Consultar_filtraPorMetodoYPaginaDeDiezRegistros()
    {
        var responsable = Mundo.CrearUsuario("General");
        const string prefijo = "Filtro auditoria ";
        for (var indice = 1; indice <= 11; indice++)
        {
            var creado = await S.Configuracion.CrearMetodoPagoAsync(
                new CrearMetodoPagoRequest { Nombre = $"{prefijo}{indice}" }, responsable.IdUsuario);
            Assert.True(creado.Success, creado.Message);
        }

        var filtrado = await S.HistorialMetodosPago.ObtenerAsync(
            $"{prefijo}1", null, responsable.IdUsuario, HistorialMetodoPagoAcciones.Creado,
            null, null, pagina: 1, tamano: 10);
        var segundaPagina = await S.HistorialMetodosPago.ObtenerAsync(
            prefijo, null, responsable.IdUsuario, HistorialMetodoPagoAcciones.Creado,
            null, null, pagina: 2, tamano: 10);

        Assert.True(filtrado.Success && segundaPagina.Success);
        Assert.Equal(3, filtrado.Data!.Total);
        Assert.Single(segundaPagina.Data!.Items);
        Assert.Equal(11, segundaPagina.Data.Total);
    }
}
