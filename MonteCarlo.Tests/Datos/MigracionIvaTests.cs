using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Datos;

public class MigracionIvaTests
{
    [SqlFact]
    public async Task Alter10ConservaVentaAnteriorYTasaAlRepetirse()
    {
        var temporal = new SqlServerFixture();
        try
        {
            await temporal.InitializeAsync();
            void Ejecutar(string sql) => SqlServerFixture.EjecutarLotes(temporal.CadenaConexion,
                ScriptsSql.Transformar(sql, temporal.NombreBase));

            Ejecutar("""
                ALTER TABLE dbo.Venta DROP COLUMN Subtotal, TasaIvaAplicada, ImporteIva;
                DELETE FROM dbo.ConfiguracionSistema WHERE Clave = 'TasaIva';
                INSERT INTO dbo.Venta (TipoVenta, Estado, Total, IdUsuarioRegistro, IdUsuarioCobro, IdMetodoPago, FechaCobro)
                SELECT 'Takeout', 'Cobrada', 123.45, MIN(IdUsuario), MIN(IdUsuario),
                       (SELECT MIN(IdMetodoPago) FROM dbo.MetodoPago), SYSDATETIME() FROM dbo.Usuario;
                """);
            var alter = ScriptsSql.Leer("alter_database-10.sql");
            Ejecutar(alter);
            Ejecutar("""
                IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave='TasaIva' AND Valor='13')
                    THROW 50001, 'Falta la tasa inicial', 1;
                UPDATE dbo.ConfiguracionSistema SET Valor='7.25' WHERE Clave='TasaIva';
                """);
            Ejecutar(alter);
            Ejecutar("""
                IF (SELECT COUNT(*) FROM dbo.ConfiguracionSistema WHERE Clave='TasaIva' AND Valor='7.25') <> 1
                    THROW 50002, 'La segunda ejecucion cambio la tasa', 1;
                IF NOT EXISTS (SELECT 1 FROM dbo.Venta WHERE Total=123.45 AND Subtotal IS NULL
                               AND TasaIvaAplicada IS NULL AND ImporteIva IS NULL)
                    THROW 50003, 'Cambio en la venta anterior', 1;
                IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Venta')
                    AND system_type_id=TYPE_ID('decimal') AND is_nullable=1 AND scale=2 AND
                    ((name IN ('Subtotal','ImporteIva') AND precision=10) OR (name='TasaIvaAplicada' AND precision=5))) <> 3
                    THROW 50004, 'Las columnas no coinciden con el esquema esperado', 1;
                """);
        }
        finally
        {
            await temporal.DisposeAsync();
        }
    }
}
