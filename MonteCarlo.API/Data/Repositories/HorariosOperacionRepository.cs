using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using System.Data;

namespace MonteCarlo.API.Data.Repositories;

public class HorariosOperacionRepository(MonteCarloDbContext context) : IHorariosOperacionRepository
{
    public Task<List<HorarioOperacion>> ObtenerTodosAsync() =>
        context.HorariosOperacion.OrderBy(h => h.DiaSemana).ThenBy(h => h.IdHorarioOperacion).ToListAsync();

    public Task<HorarioOperacion?> ObtenerPorDiaAsync(byte diaSemana) =>
        context.HorariosOperacion.Where(h => h.DiaSemana == diaSemana)
            .OrderBy(h => h.IdHorarioOperacion).FirstOrDefaultAsync();

    public Task<List<HorarioOperacion>> ObtenerPorDiasAsync(IEnumerable<byte> diasSemana)
    {
        var dias = diasSemana.Distinct().ToList();
        return context.HorariosOperacion
            .Where(h => dias.Contains(h.DiaSemana))
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.IdHorarioOperacion)
            .ToListAsync();
    }

    public void Agregar(HorarioOperacion horarioOperacion) => context.HorariosOperacion.Add(horarioOperacion);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();

    public async Task<List<byte>> ValidarCierresYGuardarCambiosAsync(IEnumerable<byte> diasSemana)
    {
        var dias = diasSemana.Distinct().ToList();

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // La comprobacion se realiza dentro de la transaccion para evitar que un
            // cierre fijo concurrente deje persistido solo una parte del lote.
            var diasCerrados = await context.CierresFijos
                .Where(c => dias.Contains(c.DiaSemana))
                .Select(c => c.DiaSemana)
                .ToListAsync();

            if (diasCerrados.Count > 0)
            {
                await transaction.RollbackAsync();
                return diasCerrados;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
