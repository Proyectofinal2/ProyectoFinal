using Microsoft.Data.SqlClient;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Datos;

/// <summary>Reglas que garantiza la base de datos (CHECK, indices unicos, FK compuesta, columna calculada).</summary>
public class RestriccionesVentaTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private const int ViolacionCheckOFk = 547;
    private const int ViolacionUnico = 2601;

    /// <summary>Ejecuta el SQL con el usuario y metodo de pago de la prueba; devuelve el error de SQL Server (0 = ok).</summary>
    private int Ejecutar(string sql, params (string, object?)[] parametros)
    {
        using var conexion = new SqlConnection(Fixture.CadenaConexion);
        conexion.Open();
        using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@u", Mundo.IdUsuario);
        comando.Parameters.AddWithValue("@m", Mundo.IdMetodoPago);
        foreach (var (nombre, valor) in parametros) comando.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        try { comando.ExecuteNonQuery(); return 0; }
        catch (SqlException ex) { return ex.Number; }
    }

    private int NuevaReserva() => Mundo.CrearReserva().IdReserva;

    private int Venta(string campos, string valores, int idReserva) =>
        Ejecutar($"INSERT INTO dbo.Venta (TipoVenta, IdReserva, IdUsuarioRegistro, {campos}) VALUES ('Consumo', @r, @u, {valores})",
            ("@r", idReserva));

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "CK_Venta")]
    public void Venta_abiertaConMetodoDePago_seRechaza() =>
        Assert.Equal(ViolacionCheckOFk, Venta("Estado, IdMetodoPago", "'Abierta', @m", NuevaReserva()));

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "CK_Venta")]
    public void Venta_cobradaConTotalYSinMetodoDePago_seRechaza() =>
        Assert.Equal(ViolacionCheckOFk,
            Venta("Estado, Total, FechaCobro, IdUsuarioCobro", "'Cobrada', 100, SYSDATETIME(), @u", NuevaReserva()));

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "CK_Venta_Abandonada")]
    public void Venta_abandonadaConMotivoFechaYUsuario_seAcepta() =>
        Assert.Equal(0, Venta("Estado, Total, MotivoAnulacion, FechaAnulacion, IdUsuarioAnulacion",
            "'Abandonada', 100, 'Se fue sin pagar', SYSDATETIME(), @u", NuevaReserva()));

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "CK_Venta_Abandonada")]
    public void Venta_abandonadaSinMotivo_seRechaza() =>
        Assert.Equal(ViolacionCheckOFk, Venta("Estado, Total, FechaAnulacion, IdUsuarioAnulacion",
            "'Abandonada', 100, SYSDATETIME(), @u", NuevaReserva()));

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "CK_Venta_Abandonada")]
    public void Venta_abandonadaConMetodoDePagoOCobro_seRechaza()
    {
        const string baja = "MotivoAnulacion, FechaAnulacion, IdUsuarioAnulacion";
        const string valoresBaja = "'Se fue sin pagar', SYSDATETIME(), @u";

        Assert.Equal(ViolacionCheckOFk,
            Venta($"Estado, Total, IdMetodoPago, {baja}", $"'Abandonada', 100, @m, {valoresBaja}", NuevaReserva()));
        Assert.Equal(ViolacionCheckOFk,
            Venta($"Estado, Total, FechaCobro, IdUsuarioCobro, {baja}", $"'Abandonada', 100, SYSDATETIME(), @u, {valoresBaja}", NuevaReserva()));
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "CK_Venta_Anulacion")]
    public void Venta_cobradaConDatosDeBaja_seRechaza() =>
        Assert.Equal(ViolacionCheckOFk, Venta("Estado, Total, FechaCobro, IdUsuarioCobro, MotivoAnulacion, FechaAnulacion, IdUsuarioAnulacion",
            "'Cobrada', 0, SYSDATETIME(), @u, 'x', SYSDATETIME(), @u", NuevaReserva()));

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "UQ_Venta_Reserva")]
    public void Venta_dosParaLaMismaReserva_seRechaza()
    {
        var reserva = NuevaReserva();
        Assert.Equal(0, Venta("Estado", "'Abierta'", reserva));

        Assert.Equal(ViolacionUnico, Venta("Estado", "'Abierta'", reserva));
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Regla", "FK_DetalleVenta_Pedido")]
    public void Linea_conPedidoDeOtraVenta_seRechaza_yElSubtotalLoCalculaLaBase()
    {
        var reservaA = NuevaReserva();
        var reservaB = NuevaReserva();
        Venta("Estado", "'Abierta'", reservaA);
        Venta("Estado", "'Abierta'", reservaB);
        var producto = Mundo.CrearProducto().IdProducto;
        int IdVenta(int r) => Mundo.VentaDe(r)!.IdVenta;
        foreach (var r in new[] { reservaA, reservaB })
            Ejecutar("INSERT INTO dbo.PedidoVenta (IdVenta, IdUsuario) VALUES (@v, @u)", ("@v", IdVenta(r)));
        var pedidoDeB = Mundo.VentaDe(reservaB) is { } b ? PedidoDe(b.IdVenta) : 0;

        const string insertar =
            "INSERT INTO dbo.DetalleVenta (IdVenta, IdPedido, IdProducto, Cantidad, PrecioUnitario) VALUES (@v, @p, @prod, 3, 1250.50)";
        var cruzada = Ejecutar(insertar, ("@v", IdVenta(reservaA)), ("@p", pedidoDeB), ("@prod", producto));
        var correcta = Ejecutar(insertar, ("@v", IdVenta(reservaB)), ("@p", pedidoDeB), ("@prod", producto));

        Assert.Equal(ViolacionCheckOFk, cruzada);
        Assert.Equal(0, correcta);
        Assert.Equal(3751.50m, Mundo.VentaDe(reservaB)!.Detalles.Single().Subtotal);
    }

    private int PedidoDe(int idVenta)
    {
        using var db = Mundo.NuevoContexto();
        return db.PedidosVenta.Single(p => p.IdVenta == idVenta).IdPedido;
    }

}
