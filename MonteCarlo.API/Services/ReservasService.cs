using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class ReservasService(IReservasRepository reservasRepository) : IReservasService
{
    private const string Pendiente = "Pendiente";
    private const string Confirmada = "Confirmada";
    private const string Cancelada = "Cancelada";

    public async Task<Result<ReservaResponse>> ObtenerPorCodigoAsync(string codigo)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());

        return reserva is null
            ? Result<ReservaResponse>.NotFound("No se encontró ninguna reserva con ese código.")
            : Result<ReservaResponse>.Ok(Mapear(reserva), "Reserva encontrada.");
    }

    public async Task<Result<ReservaResponse>> CancelarAsync(string codigo)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());

        if (reserva is null)
        {
            return Result<ReservaResponse>.NotFound("No se encontró ninguna reserva con ese código.");
        }

        if (!PuedeCancelarse(reserva))
        {
            return Result<ReservaResponse>.Conflict(
                $"La reserva está en estado \"{reserva.EstadoReserva.Nombre}\" y no puede cancelarse.");
        }

        var estadoCancelada = await reservasRepository.ObtenerEstadoAsync(Cancelada);
        if (estadoCancelada is null)
        {
            return Result<ReservaResponse>.InternalError("El estado \"Cancelada\" no está configurado.");
        }

        // Cambiar de estado y soltar la mesa: sin IdMesa la reserva deja de ocupar
        // su franja horaria y la mesa vuelve a estar disponible para otros clientes.
        reserva.EstadoReserva = estadoCancelada;
        reserva.Mesa = null;
        reserva.IdMesa = null;
        reserva.MotivoCancelacion = "Cancelada por el cliente";

        await reservasRepository.GuardarCambiosAsync();

        return Result<ReservaResponse>.Ok(Mapear(reserva), "Tu reserva fue cancelada y la mesa quedó liberada.");
    }

    private static bool PuedeCancelarse(Reserva reserva) =>
        reserva.EstadoReserva.Nombre is Pendiente or Confirmada;

    private static ReservaResponse Mapear(Reserva reserva) => new()
    {
        CodigoReserva = reserva.CodigoReserva,
        NombreCliente = $"{reserva.Cliente.Nombre} {reserva.Cliente.Apellido}".Trim(),
        FechaReserva = reserva.FechaReserva,
        HoraReserva = reserva.HoraReserva,
        CantidadPersonas = reserva.CantidadPersonas,
        Estado = reserva.EstadoReserva.Nombre,
        NumeroMesa = reserva.Mesa?.NumeroMesa,
        PuedeCancelarse = PuedeCancelarse(reserva)
    };
}
