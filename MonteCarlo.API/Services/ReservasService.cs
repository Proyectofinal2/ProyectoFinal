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
    private static readonly TimeSpan IntervaloLlegada = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DuracionReserva = TimeSpan.FromMinutes(90);

    public async Task<Result<DisponibilidadFechaResponse>> ObtenerDisponibilidadAsync(int cantidadPersonas, DateOnly fecha)
    {
        if (cantidadPersonas <= 0)
        {
            return Result<DisponibilidadFechaResponse>.BadRequest("La cantidad de personas debe ser mayor que cero.");
        }

        var datos = await ObtenerDatosDisponibilidadAsync();
        return Result<DisponibilidadFechaResponse>.Ok(
            CalcularDisponibilidad(fecha, datos, DateTime.Now),
            "Disponibilidad obtenida exitosamente.");
    }

    public async Task<Result<DisponibilidadMesResponse>> ObtenerDisponibilidadMesAsync(int cantidadPersonas, int anio, int mes)
    {
        if (cantidadPersonas <= 0)
        {
            return Result<DisponibilidadMesResponse>.BadRequest("La cantidad de personas debe ser mayor que cero.");
        }

        if (anio is < 1 or > 9999 || mes is < 1 or > 12)
        {
            return Result<DisponibilidadMesResponse>.BadRequest("El mes solicitado no es valido.");
        }

        var inicio = new DateOnly(anio, mes, 1);
        var fin = new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes));
        var datos = await ObtenerDatosDisponibilidadAsync();
        var ahora = DateTime.Now;
        var fechas = new List<DisponibilidadFechaResponse>();

        for (var dia = inicio; dia <= fin; dia = dia.AddDays(1))
        {
            var disponibilidad = CalcularDisponibilidad(dia, datos, ahora);
            fechas.Add(new DisponibilidadFechaResponse
            {
                Fecha = disponibilidad.Fecha,
                Disponible = disponibilidad.Disponible,
                MotivoNoDisponible = disponibilidad.MotivoNoDisponible
            });
        }

        return Result<DisponibilidadMesResponse>.Ok(new DisponibilidadMesResponse
        {
            CantidadPersonas = cantidadPersonas,
            Anio = anio,
            Mes = mes,
            Fechas = fechas
        }, "Disponibilidad mensual obtenida exitosamente.");
    }

    public async Task<Result<ReservaResponse>> ObtenerPorCodigoAsync(string codigo)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());

        return reserva is null
            ? Result<ReservaResponse>.NotFound("No se encontro ninguna reserva con ese codigo.")
            : Result<ReservaResponse>.Ok(Mapear(reserva), "Reserva encontrada.");
    }

    public async Task<Result<ReservaResponse>> CancelarAsync(string codigo)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());

        if (reserva is null)
        {
            return Result<ReservaResponse>.NotFound("No se encontro ninguna reserva con ese codigo.");
        }

        if (!PuedeCancelarse(reserva))
        {
            return Result<ReservaResponse>.Conflict(
                $"La reserva esta en estado \"{reserva.EstadoReserva.Nombre}\" y no puede cancelarse.");
        }

        var estadoCancelada = await reservasRepository.ObtenerEstadoAsync(Cancelada);
        if (estadoCancelada is null)
        {
            return Result<ReservaResponse>.InternalError("El estado \"Cancelada\" no esta configurado.");
        }

        reserva.EstadoReserva = estadoCancelada;
        reserva.Mesa = null;
        reserva.IdMesa = null;
        reserva.MotivoCancelacion = "Cancelada por el cliente";

        await reservasRepository.GuardarCambiosAsync();

        return Result<ReservaResponse>.Ok(Mapear(reserva), "Tu reserva fue cancelada y la mesa quedo liberada.");
    }

    private async Task<DatosDisponibilidad> ObtenerDatosDisponibilidadAsync()
    {
        var cierres = await reservasRepository.ObtenerDiasCierreFijoAsync();
        var horarios = await reservasRepository.ObtenerHorariosOperacionAsync();

        return new DatosDisponibilidad(cierres.ToHashSet(), horarios);
    }

    private static DisponibilidadFechaResponse CalcularDisponibilidad(
        DateOnly fecha,
        DatosDisponibilidad datos,
        DateTime ahora)
    {
        var respuesta = new DisponibilidadFechaResponse { Fecha = fecha };
        var hoy = DateOnly.FromDateTime(ahora);

        if (fecha < hoy)
        {
            respuesta.MotivoNoDisponible = "No se pueden seleccionar fechas pasadas.";
            return respuesta;
        }

        var diaSemana = (byte)fecha.DayOfWeek;
        if (datos.CierresFijos.Contains(diaSemana))
        {
            respuesta.MotivoNoDisponible = "El restaurante permanece cerrado este dia.";
            return respuesta;
        }

        // HU-CFG-003 can add a date-specific closure check here before the schedule lookup.
        var horario = datos.Horarios.FirstOrDefault(h => h.DiaSemana == diaSemana && h.Activo);
        if (horario is null)
        {
            respuesta.MotivoNoDisponible = "No hay horario de operacion configurado para este dia.";
            return respuesta;
        }

        var horaActual = TimeOnly.FromDateTime(ahora);

        for (var hora = horario.HoraApertura;
             hora.ToTimeSpan() + DuracionReserva <= horario.HoraCierre.ToTimeSpan();
             hora = hora.Add(IntervaloLlegada))
        {
            if (fecha == hoy && hora <= horaActual)
            {
                continue;
            }

            respuesta.HorariosDisponibles.Add(hora);
        }

        respuesta.Disponible = respuesta.HorariosDisponibles.Count > 0;
        if (!respuesta.Disponible)
        {
            respuesta.MotivoNoDisponible = "No hay horarios disponibles para esta fecha.";
        }

        return respuesta;
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

    private sealed record DatosDisponibilidad(
        HashSet<byte> CierresFijos,
        List<HorarioOperacion> Horarios);
}