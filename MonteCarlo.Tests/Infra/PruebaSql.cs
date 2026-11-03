using MonteCarlo.Tests.Builders;

namespace MonteCarlo.Tests.Infra;

/// <summary>Base de las pruebas de integracion: una base temporal compartida y datos propios por prueba.</summary>
[Collection(SqlServerCollection.Nombre)]
public abstract class PruebaSql : IDisposable
{
    protected PruebaSql(SqlServerFixture fixture)
    {
        Fixture = fixture;
        // Sin SQL Server las pruebas se omiten (SqlFact); el constructor no debe tocar la base.
        Mundo = SqlDisponibilidad.Disponible ? new Mundo(fixture) : null!;
    }

    protected SqlServerFixture Fixture { get; }
    protected Mundo Mundo { get; }
    protected Sesion S => Mundo.Servicios;

    public void Dispose() => Mundo?.Dispose();
}
