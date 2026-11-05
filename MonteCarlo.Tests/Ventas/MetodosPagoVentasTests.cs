using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Ventas;

/// <summary>
/// Un metodo de pago deshabilitado deja de ofrecerse al cobrar, pero las ventas que lo usaron lo conservan y el filtro
/// del listado de ventas debe poder elegirlo (HU-CFG-007, HU-CON-004).
/// </summary>
public class MetodosPagoVentasTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private int CrearMetodo(bool activo)
    {
        using var db = Mundo.NuevoContexto();
        var metodo = new MetodoPago { Nombre = $"M{Guid.NewGuid():N}"[..20], Activo = activo };
        db.MetodosPago.Add(metodo);
        db.SaveChanges();
        return metodo.IdMetodoPago;
    }

    private void Deshabilitar(int idMetodo)
    {
        using var db = Mundo.NuevoContexto();
        db.MetodosPago.Single(m => m.IdMetodoPago == idMetodo).Activo = false;
        db.SaveChanges();
    }

    [SqlFact]
    [Trait("HU", "CON-004")]
    public async Task ObtenerMetodosPago_porDefectoSoloActivos_yConTodosTambienLosDeshabilitados()
    {
        var activo = CrearMetodo(activo: true);
        var inactivo = CrearMetodo(activo: false);

        var soloActivos = (await S.Ventas.ObtenerMetodosPagoAsync()).Data!;
        var todos = (await S.Ventas.ObtenerMetodosPagoAsync(incluirInactivos: true)).Data!;

        Assert.Contains(soloActivos, m => m.IdMetodoPago == activo);
        Assert.DoesNotContain(soloActivos, m => m.IdMetodoPago == inactivo);
        Assert.Contains(todos, m => m.IdMetodoPago == activo && m.Activo);
        Assert.Contains(todos, m => m.IdMetodoPago == inactivo && !m.Activo);
    }

    [SqlFact]
    [Trait("HU", "CON-004")]
    public async Task Listado_ventaCobradaConMetodoQueSeDeshabilitoDespues_informaQueYaNoEstaActivo()
    {
        var idMetodo = CrearMetodo(activo: true);
        var producto = Mundo.CrearProducto(precio: 1000m);
        var venta = (await S.Ventas.RegistrarParaLlevarAsync(new ParaLlevarRequest
        {
            ClaveIdempotencia = Guid.NewGuid(),
            IdMetodoPago = idMetodo,
            TotalEsperado = 1000m,
            Lineas = [new LineaPedidoRequest { IdProducto = producto.IdProducto, Cantidad = 1 }]
        }, Mundo.IdUsuario)).Data!;

        Deshabilitar(idMetodo);

        // Sesion nueva: cada solicitud real tiene su propio contexto, sin el metodo ya cargado como activo.
        using var consulta = Mundo.NuevaSesion();
        var fila = (await consulta.Ventas.ObtenerListadoAsync(new FiltroVentasRequest { IdMetodoPago = idMetodo })).Data!.Single();
        var detalle = (await consulta.Ventas.ObtenerDetalleAsync(venta.IdVenta!.Value)).Data!;
        Assert.Equal(venta.IdVenta, fila.IdVenta);
        Assert.False(fila.MetodoPagoActivo);
        Assert.False(detalle.MetodoPagoActivo);
    }
}
