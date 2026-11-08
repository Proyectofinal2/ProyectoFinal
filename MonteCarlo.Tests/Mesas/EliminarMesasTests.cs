using Microsoft.EntityFrameworkCore;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Mesas;

/// <summary>La mesa no se puede borrar si ReservaMesa conserva su historial de asignacion.</summary>
public class EliminarMesasTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    [SqlFact]
    public async Task Mesa_inexistente_conserva_la_respuesta_controlada_actual()
    {
        var resultado = await S.Mesas.EliminarAsync(int.MaxValue);

        Assert.False(resultado.Success);
        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("No se encontro la mesa.", resultado.Message);
    }

    [SqlFact]
    public async Task Mesa_sin_reservas_se_elimina_fisicamente()
    {
        var mesa = Mundo.CrearMesa();

        var resultado = await S.Mesas.EliminarAsync(mesa.IdMesa);

        Assert.True(resultado.Success, resultado.Message);
        await using var db = Mundo.NuevoContexto();
        Assert.False(await db.Mesas.AnyAsync(m => m.IdMesa == mesa.IdMesa));
    }

    [SqlFact]
    public async Task Mesa_con_reservas_no_se_elimina_y_las_reservas_se_conservan()
    {
        var mesa = Mundo.CrearMesa();
        var reserva = Mundo.CrearReserva([mesa]);

        var resultado = await S.Mesas.EliminarAsync(mesa.IdMesa);

        Assert.False(resultado.Success);
        Assert.Equal(409, resultado.StatusCode);
        Assert.Equal("No se puede eliminar esta mesa porque tiene reservas asociadas.", resultado.Message);

        await using var db = Mundo.NuevoContexto();
        Assert.True(await db.Mesas.AnyAsync(m => m.IdMesa == mesa.IdMesa));
        Assert.True(await db.Reservas.AnyAsync(r => r.IdReserva == reserva.IdReserva));
        Assert.True(await db.ReservasMesa.AnyAsync(rm => rm.IdMesa == mesa.IdMesa && rm.IdReserva == reserva.IdReserva));
    }
}
