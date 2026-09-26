using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class MesasRepository(MonteCarloDbContext context) : IMesasRepository
{
    public Task<List<Mesa>> ObtenerDisponiblesPorCapacidadAsync(int cantidadPersonas) =>
        context.Mesas
            .AsNoTracking()
            .Where(m => m.Estado == "Disponible" && m.Capacidad >= cantidadPersonas)
            .OrderBy(m => m.Capacidad)
            .ThenBy(m => m.NumeroMesa)
            .ToListAsync();

    public Task<List<Reserva>> ObtenerReservasActivasPorMesaYFechaAsync(int idMesa, DateOnly fecha) =>
        context.Reservas
            .Include(r => r.EstadoReserva)
            .Where(r => r.IdMesa == idMesa
                        && r.FechaReserva == fecha
                        && (r.EstadoReserva.Nombre == "Pendiente"
                            || r.EstadoReserva.Nombre == "Confirmada"))
            .ToListAsync();
}