using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class MesasRepository(MonteCarloDbContext context) : IMesasRepository
{
    public Task<List<Mesa>> ObtenerMesasHabilitadasAsync() =>
        context.Mesas
            .AsNoTracking()
            .Where(m => m.Estado == "Disponible")
            .OrderBy(m => m.NumeroMesa)
            .ToListAsync();

    public async Task<Dictionary<int, List<TimeOnly>>> ObtenerHorasReservadasPorFechaAsync(DateOnly fecha)
    {
        var rango = await ObtenerHorasReservadasPorRangoAsync(fecha, fecha);
        return rango.GetValueOrDefault(fecha, []);
    }

    public async Task<Dictionary<DateOnly, Dictionary<int, List<TimeOnly>>>> ObtenerHorasReservadasPorRangoAsync(
        DateOnly inicio, DateOnly fin)
    {
        var filas = await context.Reservas
            .Where(r => r.FechaReserva >= inicio && r.FechaReserva <= fin
                        && (r.EstadoReserva.Nombre == "Pendiente" || r.EstadoReserva.Nombre == "Confirmada"))
            .SelectMany(r => r.ReservaMesas, (r, rm) => new { r.FechaReserva, rm.IdMesa, r.HoraReserva })
            .ToListAsync();

        return filas
            .GroupBy(f => f.FechaReserva)
            .ToDictionary(
                dia => dia.Key,
                dia => dia
                    .GroupBy(f => f.IdMesa)
                    .ToDictionary(mesa => mesa.Key, mesa => mesa.Select(f => f.HoraReserva).ToList()));
    }

    public Task<List<Mesa>> ObtenerPorIdsAsync(List<int> idsMesa) =>
        context.Mesas
            .AsNoTracking()
            .Where(m => idsMesa.Contains(m.IdMesa))
            .ToListAsync();
}