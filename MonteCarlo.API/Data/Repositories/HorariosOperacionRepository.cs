using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class HorariosOperacionRepository(MonteCarloDbContext context) : IHorariosOperacionRepository
{
    public Task<List<HorarioOperacion>> ObtenerTodosAsync() =>
        context.HorariosOperacion.OrderBy(h => h.DiaSemana).ThenBy(h => h.IdHorarioOperacion).ToListAsync();

    public Task<HorarioOperacion?> ObtenerPorDiaAsync(byte diaSemana) =>
        context.HorariosOperacion.Where(h => h.DiaSemana == diaSemana)
            .OrderBy(h => h.IdHorarioOperacion).FirstOrDefaultAsync();

    public void Agregar(HorarioOperacion horarioOperacion) => context.HorariosOperacion.Add(horarioOperacion);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
