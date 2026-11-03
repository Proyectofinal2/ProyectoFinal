using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data;

namespace MonteCarlo.Tests.Infra;

/// <summary>
/// Base de datos temporal de las pruebas de integracion: se crea a partir de una COPIA EN MEMORIA de database.sql
/// con el nombre cambiado (nunca se ejecuta el script tal cual: borra tablas y usa la base real) y se elimina
/// siempre al terminar. La base real "MonteCarlo" no se toca ni se lee.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string VariableEntorno = "MONTECARLO_TEST_SQL";
    private const string CadenaPorDefecto =
        @"Data Source=DESKTOP-2KE6JBI\SQLEXPRESS;Integrated Security=True;Trust Server Certificate=True";

    public string NombreBase { get; } = "MonteCarlo_test_" + Guid.NewGuid().ToString("N")[..8];
    public string CadenaBase => SqlDisponibilidad.CadenaBase;
    public string CadenaConexion => new SqlConnectionStringBuilder(CadenaBase) { InitialCatalog = NombreBase }.ConnectionString;
    private string CadenaMaster => new SqlConnectionStringBuilder(CadenaBase) { InitialCatalog = "master" }.ConnectionString;

    public async Task InitializeAsync()
    {
        if (!SqlDisponibilidad.Disponible) return;
        EjecutarLotes(CadenaMaster, ScriptsSql.Transformar(ScriptsSql.Leer("database.sql"), NombreBase));
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!SqlDisponibilidad.Disponible) return;
        await EliminarBaseAsync(CadenaMaster, NombreBase);
    }

    public MonteCarloDbContext NuevoContexto() =>
        new(new DbContextOptionsBuilder<MonteCarloDbContext>().UseSqlServer(CadenaConexion).Options);

    public static void EjecutarLotes(string cadena, IEnumerable<string> lotes)
    {
        using var conexion = new SqlConnection(cadena);
        conexion.Open();
        foreach (var lote in lotes)
        {
            using var comando = new SqlCommand("SET QUOTED_IDENTIFIER ON;\n" + lote, conexion) { CommandTimeout = 120 };
            comando.ExecuteNonQuery();
        }
    }

    /// <summary>Elimina una base temporal. Rechaza cualquier nombre que no empiece con MonteCarlo_test_.</summary>
    public static async Task EliminarBaseAsync(string cadenaMaster, string nombre)
    {
        if (!Regex.IsMatch(nombre, "^MonteCarlo_test_[0-9a-f]{8}$"))
            throw new InvalidOperationException($"Se rechaza eliminar la base '{nombre}': no es una base temporal de pruebas.");

        SqlConnection.ClearAllPools();
        await using var conexion = new SqlConnection(cadenaMaster);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(
            $"IF DB_ID(N'{nombre}') IS NOT NULL BEGIN ALTER DATABASE [{nombre}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombre}]; END",
            conexion);
        await comando.ExecuteNonQueryAsync();
    }
}

public static class SqlDisponibilidad
{
    public static string CadenaBase { get; } =
        Environment.GetEnvironmentVariable(SqlServerFixture.VariableEntorno)
        ?? @"Data Source=DESKTOP-2KE6JBI\SQLEXPRESS;Integrated Security=True;Trust Server Certificate=True";

    public static bool Disponible { get; } = Probar();

    public const string MotivoOmision =
        "SQL Server no esta disponible (configura la variable de entorno MONTECARLO_TEST_SQL sin Initial Catalog).";

    private static bool Probar()
    {
        try
        {
            var cadena = new SqlConnectionStringBuilder(CadenaBase) { InitialCatalog = "master", ConnectTimeout = 5 };
            using var conexion = new SqlConnection(cadena.ConnectionString);
            conexion.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>[Fact] que se omite (con motivo) cuando no hay SQL Server en vez de fallar.</summary>
public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (!SqlDisponibilidad.Disponible) Skip = SqlDisponibilidad.MotivoOmision;
    }
}

/// <summary>[Theory] que se omite (con motivo) cuando no hay SQL Server.</summary>
public sealed class SqlTheoryAttribute : TheoryAttribute
{
    public SqlTheoryAttribute()
    {
        if (!SqlDisponibilidad.Disponible) Skip = SqlDisponibilidad.MotivoOmision;
    }
}

[CollectionDefinition(Nombre)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Nombre = "SqlServer";
}
