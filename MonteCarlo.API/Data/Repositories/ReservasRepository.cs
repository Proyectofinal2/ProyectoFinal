using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class ReservasRepository(MonteCarloDbContext context) : IReservasRepository
{
	public Task<Reserva?> ObtenerPorCodigoAsync(string codigo) =>
		context.Reservas
			.Include(r => r.Cliente)
			.Include(r => r.Mesa)
			.Include(r => r.EstadoReserva)
			.FirstOrDefaultAsync(r => r.CodigoReserva == codigo);

	public Task<EstadoReserva?> ObtenerEstadoAsync(string nombre) =>
		context.EstadosReserva.FirstOrDefaultAsync(e => e.Nombre == nombre);

	public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
