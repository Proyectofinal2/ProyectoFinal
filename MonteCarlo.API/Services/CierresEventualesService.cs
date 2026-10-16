using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class CierresEventualesService(
    ICierresEventualesRepository cierresEventualesRepository,
    IReservasRepository reservasRepository) : ICierresEventualesService
{
    private const string Pendiente = "Pendiente";
    private const string Confirmada = "Confirmada";
    private const string Cancelada = "Cancelada";

    public async Task<Result<List<CierreEventualResponse>>> ObtenerAsync()
    {
        var cierres = await cierresEventualesRepository.ObtenerDesdeAsync(DateOnly.FromDateTime(DateTime.Now));
        return Result<List<CierreEventualResponse>>.Ok(
            cierres.Select(Mapear).ToList(), "Cierres eventuales obtenidos exitosamente.");
    }

    public async Task<Result<CierreEventualResponse>> RegistrarAsync(
        DateOnly fecha, string motivo, bool cancelarReservas, int idUsuario)
    {
        if (fecha < DateOnly.FromDateTime(DateTime.Now))
            return Result<CierreEventualResponse>.BadRequest("No se puede registrar un cierre en una fecha pasada.");

        if (await cierresEventualesRepository.ExistePorFechaAsync(fecha))
            return Result<CierreEventualResponse>.Conflict("Ya existe un cierre registrado para esa fecha.");

        // Solo se cancelan las reservas que todavia no fueron atendidas.
        var reservas = (await reservasRepository.ObtenerPorFechaAsync(fecha))
            .Where(r => r.EstadoReserva.Nombre is Pendiente or Confirmada && r.FechaAtencion is null)
            .ToList();

        if (reservas.Count > 0 && !cancelarReservas)
        {
            return Result<CierreEventualResponse>.Conflict(
                $"Hay {reservas.Count} reserva(s) en esa fecha. Confirma su cancelacion para registrar el cierre.");
        }

        var estadoCancelada = await reservasRepository.ObtenerEstadoAsync(Cancelada);
        if (estadoCancelada is null)
            return Result<CierreEventualResponse>.InternalError("El estado \"Cancelada\" no esta configurado.");

        var cierre = new CierreEventual { Fecha = fecha, Motivo = motivo.Trim(), IdUsuarioCreacion = idUsuario };
        cierresEventualesRepository.Agregar(cierre);

        foreach (var reserva in reservas)
        {
            reserva.EstadoReserva = estadoCancelada;
            reserva.ReservaMesas.Clear();
            reserva.MotivoCancelacion = "Cierre administrativo";
            reserva.CierreEventual = cierre;
        }

        cierresEventualesRepository.AgregarHistorial(new HistorialCierre
        {
            TipoCierre = "Eventual",
            Accion = "Registrado",
            Fecha = DateTime.Now,
            IdUsuario = idUsuario,
            Motivo = cierre.Motivo,
            FechaCierre = fecha,
            CierreEventual = cierre
        });

        await cierresEventualesRepository.GuardarCambiosAsync();

        return Result<CierreEventualResponse>.Ok(Mapear(cierre),
            reservas.Count == 0
                ? "El cierre fue registrado correctamente."
                : $"El cierre fue registrado y se cancelaron {reservas.Count} reserva(s).");
    }

    public async Task<Result<CierreEventualResponse>> EliminarAsync(int idCierreEventual, int idUsuario)
    {
        var cierre = await cierresEventualesRepository.ObtenerPorIdAsync(idCierreEventual);
        if (cierre is null)
            return Result<CierreEventualResponse>.NotFound("No se encontro el cierre indicado.");

        if (cierre.Fecha < DateOnly.FromDateTime(DateTime.Now))
            return Result<CierreEventualResponse>.Conflict("No se puede quitar un cierre de una fecha pasada.");

        cierre.Activo = false;
        cierre.FechaEliminacion = DateTime.Now;
        cierre.IdUsuarioEliminacion = idUsuario;

        cierresEventualesRepository.AgregarHistorial(new HistorialCierre
        {
            TipoCierre = "Eventual",
            Accion = "Eliminado",
            Fecha = DateTime.Now,
            IdUsuario = idUsuario,
            Motivo = cierre.Motivo,
            FechaCierre = cierre.Fecha,
            IdCierreEventual = cierre.IdCierreEventual
        });

        await cierresEventualesRepository.GuardarCambiosAsync();

        return Result<CierreEventualResponse>.Ok(Mapear(cierre),
            "El cierre fue quitado. Las reservas que ya se cancelaron por este cierre no se restablecen.");
    }

    private static CierreEventualResponse Mapear(CierreEventual cierre) => new()
    {
        IdCierreEventual = cierre.IdCierreEventual,
        Fecha = cierre.Fecha,
        Motivo = cierre.Motivo,
        UsuarioCreacion = cierre.UsuarioCreacion?.NombreCompleto ?? string.Empty
    };
}
