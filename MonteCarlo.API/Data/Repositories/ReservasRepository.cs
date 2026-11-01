using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Data.Repositories;

public class ReservasRepository(MonteCarloDbContext context) : IReservasRepository
{
    public Task<Reserva?> ObtenerPorCodigoAsync(string codigo) =>
        context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.ReservaMesas).ThenInclude(rm => rm.Mesa)
            .Include(r => r.EstadoReserva)
            .FirstOrDefaultAsync(r => r.CodigoReserva == codigo);

    public Task<Reserva?> ObtenerPorIdAsync(int id) =>
        context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.EstadoReserva)
            .Include(r => r.ReservaMesas)
            .FirstOrDefaultAsync(r => r.IdReserva == id);

    public Task<EstadoReserva?> ObtenerEstadoAsync(string nombre) =>
        context.EstadosReserva.FirstOrDefaultAsync(e => e.Nombre == nombre);

    public Task<List<EstadoReservaReporteResponse>> ObtenerEstadosReporteAsync() =>
        context.EstadosReserva
            .AsNoTracking()
            .OrderBy(e => e.IdEstadoReserva)
            .Select(e => new EstadoReservaReporteResponse { Nombre = e.Nombre })
            .ToListAsync();

    public async Task<ResumenReporteReservasDatos> ObtenerReporteAsync(
        DateOnly fechaInicio, DateOnly fechaFin, string? estado)
    {
        var consulta = context.Reservas
            .AsNoTracking()
            .Where(r => r.FechaReserva >= fechaInicio && r.FechaReserva <= fechaFin);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(r => r.EstadoReserva.Nombre == estado);
        }

        var resumen = await consulta
            .GroupBy(_ => 1)
            .Select(grupo => new ResumenReporteReservasDatos
            {
                TotalReservas = grupo.Count(),
                PersonasReservadas = grupo.Sum(r => r.CantidadPersonas),
                ReservasAtendidas = grupo.Count(r => r.FechaAtencion != null),
                NoShows = grupo.Count(r => r.EstadoReserva.Nombre == "No Show"),
                Cancelaciones = grupo.Count(r => r.EstadoReserva.Nombre == "Cancelada")
            })
            .FirstOrDefaultAsync()
            ?? new ResumenReporteReservasDatos();

        resumen.ConteosPorEstado = await consulta
            .GroupBy(r => r.EstadoReserva.Nombre)
            .Select(grupo => new ConteoEstadoReservaDatos
            {
                Estado = grupo.Key,
                CantidadReservas = grupo.Count()
            })
            .ToListAsync();

        return resumen;
    }

    public Task<List<byte>> ObtenerDiasCierreFijoAsync() =>
        context.CierresFijos.AsNoTracking().Select(c => c.DiaSemana).ToListAsync();

    public Task<List<DateOnly>> ObtenerFechasCierreEventualAsync(DateOnly desde) =>
        context.CierresEventuales.AsNoTracking().Where(c => c.Fecha >= desde).Select(c => c.Fecha).ToListAsync();

    public Task<List<HorarioOperacion>> ObtenerHorariosOperacionAsync() =>
        context.HorariosOperacion.AsNoTracking().ToListAsync();

    public Task<List<Reserva>> ObtenerPorFechaAsync(DateOnly fecha) =>
        context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.EstadoReserva)
            .Include(r => r.ReservaMesas).ThenInclude(rm => rm.Mesa)
            .Where(r => r.FechaReserva == fecha)
            .OrderBy(r => r.HoraReserva)
            .ToListAsync();

    /// <summary>HU-OPE-005: busca por nombre del cliente o teléfono, desde hoy en adelante.</summary>
    public Task<List<Reserva>> BuscarPorNombreOTelefonoAsync(string termino, int limite)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var patron = $"%{termino.Trim()}%";

        return context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.EstadoReserva)
            .Include(r => r.ReservaMesas).ThenInclude(rm => rm.Mesa)
            .Where(r => r.FechaReserva >= hoy
                        && r.EstadoReserva.Nombre != "Cancelada"
                        && !r.EsWalkIn  // walk-ins no son reservas formales
                        && (EF.Functions.Like(r.Cliente.Nombre, patron)
                            || EF.Functions.Like(r.Cliente.Apellido ?? "", patron)
                            || EF.Functions.Like(r.Cliente.Telefono, patron)))
            .OrderBy(r => r.FechaReserva)
            .ThenBy(r => r.HoraReserva)
            .Take(limite)
            .ToListAsync();
    }

    public Task<bool> ExisteCodigoAsync(string codigo) =>
        context.Reservas.AnyAsync(r => r.CodigoReserva == codigo);

    public void Agregar(Reserva reserva) => context.Reservas.Add(reserva);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();

    public Task<List<Reserva>> ObtenerConfirmadasPorFechaAsync(DateOnly fecha) =>
        context.Reservas
            .AsNoTracking()
            .Include(r => r.ReservaMesas)
            .Where(r => r.FechaReserva == fecha && r.EstadoReserva.Nombre == "Confirmada")
            .ToListAsync();

    public Task<Reserva?> ObtenerConfirmadaActivaPorMesaAsync(int idMesa, DateOnly fecha) =>
        context.Reservas
            .Include(r => r.ReservaMesas)
            .Where(r => r.FechaReserva == fecha
                        && r.EstadoReserva.Nombre == "Confirmada"
                        && r.FechaAtencion != null
                        && r.ReservaMesas.Any(rm => rm.IdMesa == idMesa))
            .OrderByDescending(r => r.FechaAtencion)
            .FirstOrDefaultAsync();
}
