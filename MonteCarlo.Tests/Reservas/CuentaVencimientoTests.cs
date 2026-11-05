using MonteCarlo.API.Services;

namespace MonteCarlo.Tests.Reservas;

/// <summary>
/// Cuando una cuenta abierta pasa a ser antigua o por cobrar (HU-CON-002): llegada + duracion del turno + margen.
/// Funcion pura: no necesita SQL Server.
/// </summary>
public class CuentaVencimientoTests
{
    private static readonly DateTime Llegada = new(2026, 11, 4, 19, 30, 0);

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void Calcular_esLaLlegadaMasLaDuracionMasElMargen()
    {
        var vence = CuentaVencimiento.Calcular(Llegada, duracionMinutos: 90, toleranciaMinutos: 30);

        Assert.Equal(new DateTime(2026, 11, 4, 21, 30, 0), vence);
    }

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void Calcular_siCambiaLaDuracionDelTurno_cambiaElVencimiento()
    {
        Assert.Equal(new DateTime(2026, 11, 4, 22, 0, 0), CuentaVencimiento.Calcular(Llegada, 120, 30));
        Assert.Equal(new DateTime(2026, 11, 4, 21, 0, 0), CuentaVencimiento.Calcular(Llegada, 60, 30));
    }

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void Calcular_conMargenCero_vencePasadoElTurno()
    {
        Assert.Equal(new DateTime(2026, 11, 4, 21, 0, 0), CuentaVencimiento.Calcular(Llegada, 90, 0));
    }

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void Calcular_cruzaLaMedianoche_sinDependerDeLosHorarios()
    {
        var tarde = new DateTime(2026, 11, 4, 23, 0, 0);

        Assert.Equal(new DateTime(2026, 11, 5, 1, 0, 0), CuentaVencimiento.Calcular(tarde, 90, 30));
    }

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void EstaVencida_justoEnElLimite_noEsAntiguaYUnTickDespuesSi()
    {
        var limite = new DateTime(2026, 11, 4, 21, 30, 0);

        Assert.False(CuentaVencimiento.EstaVencida(Llegada, 90, 30, limite));
        Assert.True(CuentaVencimiento.EstaVencida(Llegada, 90, 30, limite.AddTicks(1)));
    }

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void EstaVencida_dentroDelTurnoOdelMargen_noEsAntigua()
    {
        Assert.False(CuentaVencimiento.EstaVencida(Llegada, 90, 30, Llegada.AddMinutes(60)));    // dentro del turno
        Assert.False(CuentaVencimiento.EstaVencida(Llegada, 90, 30, Llegada.AddMinutes(110)));   // pasado el turno, dentro del margen
        Assert.True(CuentaVencimiento.EstaVencida(Llegada, 90, 30, Llegada.AddMinutes(125)));    // pasado el margen
    }

    [Fact]
    [Trait("HU", "CON-002"), Trait("Escenario", "13")]
    public void EstaVencida_cuentaDeUnDiaAnterior_esAntigua()
    {
        Assert.True(CuentaVencimiento.EstaVencida(Llegada, 90, 30, new DateTime(2026, 11, 5, 9, 0, 0)));
    }
}
