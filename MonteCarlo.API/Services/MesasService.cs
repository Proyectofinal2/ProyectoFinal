using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class MesasService(
    IMesasRepository mesasRepository,
    IReservasRepository reservasRepository,
    IConfiguracionService configuracionService) : IMesasService
{
    private const string Deshabilitada = "Deshabilitada";

    // ============================================================
    // HU-MES-002 / HU-OPE-004: listado en vivo del canvas.
    // ============================================================

    public async Task<Result<List<MesaResponse>>> ListarAsync(bool incluirDeshabilitadas)
    {
        var estadosEnVivo = await ObtenerEstadosEnVivoAsync(incluirDeshabilitadas);
        var respuesta = estadosEnVivo.Select(x => x.Respuesta).ToList();

        return Result<List<MesaResponse>>.Ok(respuesta, "Mesas obtenidas exitosamente.");
    }

    /// <summary>Resume la ocupacion actual sin duplicar las reglas de estado del canvas.</summary>
    public async Task<Result<ResumenMesasResponse>> ObtenerResumenAsync()
    {
        var estadosEnVivo = await ObtenerEstadosEnVivoAsync(incluirDeshabilitadas: true);
        var mesasHabilitadas = estadosEnVivo
            .Where(x => x.Mesa.Estado != Deshabilitada)
            .ToList();

        var mesasOcupadas = mesasHabilitadas.Count(x => x.Respuesta.Estado == "Ocupada");
        var totalHabilitadas = mesasHabilitadas.Count;

        var resumen = new ResumenMesasResponse
        {
            MesasHabilitadas = totalHabilitadas,
            MesasDisponibles = mesasHabilitadas.Count(x => x.Respuesta.Estado == "Disponible"),
            MesasOcupadas = mesasOcupadas,
            PorcentajeOcupacion = totalHabilitadas == 0
                ? 0
                : Math.Round(mesasOcupadas * 100m / totalHabilitadas, 1),
            MensajeContextual = totalHabilitadas == 0
                ? "No hay mesas habilitadas actualmente."
                : null
        };

        return Result<ResumenMesasResponse>.Ok(resumen, "Resumen de mesas obtenido exitosamente.");
    }

    /// <summary>
    /// Obtiene las mesas y aplica una unica vez la logica de estado actual usada por
    /// el canvas y el dashboard. La ocupacion no se recalcula fuera de este servicio.
    /// </summary>
    private async Task<List<(Mesa Mesa, MesaResponse Respuesta)>> ObtenerEstadosEnVivoAsync(bool incluirDeshabilitadas)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var ahora = DateTime.Now;

        var mesas = incluirDeshabilitadas
            ? await mesasRepository.ObtenerTodasAsync()
            : await mesasRepository.ObtenerMesasHabilitadasAsync();
        var reservasHoy = await reservasRepository.ObtenerConfirmadasPorFechaAsync(hoy);
        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var ventanaMinutos = await configuracionService.ObtenerValorVentanaAlertaReservaProximaAsync();
        var contexto = new ContextoEstadoEnVivo(hoy, ahora, duracionMinutos, ventanaMinutos);

        return mesas
            .Select(mesa => (Mesa: mesa, Respuesta: MapearEstadoEnVivo(mesa, reservasHoy, contexto)))
            .ToList();
    }

    private readonly record struct ContextoEstadoEnVivo(DateOnly Hoy, DateTime Ahora, int DuracionMinutos, int VentanaMinutos);

    /// <summary>Cruza el Estado persistido de la mesa con las reservas Confirmadas del día -- "Ocupada" nunca se persiste.</summary>
    private static MesaResponse MapearEstadoEnVivo(Mesa mesa, List<Reserva> reservasHoy, ContextoEstadoEnVivo contexto)
    {
        var (hoy, ahora, duracionMinutos, ventanaMinutos) = contexto;

        var reservasDeEstaMesa = reservasHoy
            .Where(r => r.ReservaMesas.Any(rm => rm.IdMesa == mesa.IdMesa))
            .ToList();

        var respuesta = new MesaResponse
        {
            IdMesa = mesa.IdMesa,
            NumeroMesa = mesa.NumeroMesa,
            Capacidad = mesa.Capacidad,
            PosX = mesa.PosX,
            PosY = mesa.PosY,
            Estado = "Disponible",
            TieneReservaActiva = reservasDeEstaMesa.Count > 0,
            EnUnionActiva = reservasDeEstaMesa.Any(r => r.ReservaMesas.Count > 1),
            DuracionReservaMinutos = duracionMinutos
        };

        var ocupante = reservasDeEstaMesa.FirstOrDefault(r => EstaOcupandoAhora(r, hoy, ahora, duracionMinutos));

        if (ocupante is not null)
        {
            respuesta.Estado = "Ocupada";
            respuesta.PersonasActuales = ocupante.CantidadPersonas;

            // Misma ventana que "proxima" abajo (VentanaAlertaReservaProximaMinutos) -- antes
            // este badge no tenia limite de tiempo, mostrando una reserva aunque faltaran horas.
            var siguiente = reservasDeEstaMesa
                .Where(r => r.IdReserva != ocupante.IdReserva && r.FechaAtencion is null)
                .Select(r => (Reserva: r, Inicio: hoy.ToDateTime(r.HoraReserva)))
                .Where(x => x.Inicio >= ahora && (x.Inicio - ahora).TotalMinutes <= ventanaMinutos)
                .OrderBy(x => x.Inicio)
                .FirstOrDefault();

            if (siguiente.Reserva is not null)
            {
                respuesta.MinutosHastaSiguienteReserva = (int)Math.Max(0, (siguiente.Inicio - ahora).TotalMinutes);
            }

            return respuesta;
        }

        if (mesa.Estado == Deshabilitada)
        {
            respuesta.Estado = Deshabilitada;
            return respuesta;
        }

        var proxima = reservasDeEstaMesa
            .Where(r => r.FechaAtencion is null)
            .Select(r => (Reserva: r, Inicio: hoy.ToDateTime(r.HoraReserva)))
            .Where(x => x.Inicio >= ahora && (x.Inicio - ahora).TotalMinutes <= ventanaMinutos)
            .OrderBy(x => x.Inicio)
            .FirstOrDefault();

        if (proxima.Reserva is not null)
        {
            respuesta.EsProxima = true;
            respuesta.MinutosProximaReserva = (int)Math.Ceiling((proxima.Inicio - ahora).TotalMinutes);
        }

        return respuesta;
    }

    /// <summary>Ocupa desde el check-in si lo hay; si no, desde la hora programada (misma ventana que EstaLibre).</summary>
    private static bool EstaOcupandoAhora(Reserva reserva, DateOnly hoy, DateTime ahora, int duracionMinutos)
    {
        var inicio = reserva.FechaAtencion ?? hoy.ToDateTime(reserva.HoraReserva);
        return ahora >= inicio && ahora < inicio.AddMinutes(duracionMinutos);
    }

    // HU-MES-001/002/003/006: CRUD minimo de mesas, alcance del
    // companero de equipo (ver MesasService para el detalle).

    /// <summary>Para CRUD sin reservas que cruzar: Duracion/Ventana no se usan.</summary>
    private static ContextoEstadoEnVivo ContextoSinReservas() =>
        new(DateOnly.FromDateTime(DateTime.Now), DateTime.Now, 0, 0);

    public async Task<Result<MesaResponse>> CrearAsync(CrearMesaRequest request)
    {
        var mesa = new Mesa
        {
            NumeroMesa = request.NumeroMesa,
            Capacidad = request.Capacidad,
            PosX = request.PosX,
            PosY = request.PosY,
            Estado = "Disponible"
        };

        mesasRepository.Agregar(mesa);
        await mesasRepository.GuardarCambiosAsync();

        return Result<MesaResponse>.Ok(
            MapearEstadoEnVivo(mesa, [], ContextoSinReservas()),
            "Mesa creada.");
    }

    public async Task<Result<MesaResponse>> ActualizarCapacidadAsync(int idMesa, ActualizarCapacidadRequest request)
    {
        var mesa = await mesasRepository.ObtenerPorIdAsync(idMesa);
        if (mesa is null)
        {
            return Result<MesaResponse>.NotFound("No se encontro la mesa.");
        }

        mesa.Capacidad = request.Capacidad;
        await mesasRepository.GuardarCambiosAsync();

        return Result<MesaResponse>.Ok(
            MapearEstadoEnVivo(mesa, [], ContextoSinReservas()),
            "Capacidad actualizada.");
    }

    public async Task<Result<MesaResponse>> ActualizarEstadoAsync(int idMesa, ActualizarEstadoRequest request)
    {
        var mesa = await mesasRepository.ObtenerPorIdAsync(idMesa);
        if (mesa is null)
        {
            return Result<MesaResponse>.NotFound("No se encontro la mesa.");
        }

        if (request.Estado is not ("Disponible" or "Deshabilitada"))
        {
            return Result<MesaResponse>.BadRequest("Estado invalido: solo se admite Disponible o Deshabilitada.");
        }

        mesa.Estado = request.Estado;
        await mesasRepository.GuardarCambiosAsync();

        return Result<MesaResponse>.Ok(
            MapearEstadoEnVivo(mesa, [], ContextoSinReservas()),
            "Estado actualizado.");
    }

    public async Task<Result<MesaResponse>> ActualizarPosicionAsync(int idMesa, ActualizarPosicionRequest request)
    {
        var mesa = await mesasRepository.ObtenerPorIdAsync(idMesa);
        if (mesa is null)
        {
            return Result<MesaResponse>.NotFound("No se encontro la mesa.");
        }

        mesa.PosX = request.PosX;
        mesa.PosY = request.PosY;
        await mesasRepository.GuardarCambiosAsync();

        return Result<MesaResponse>.Ok(
            MapearEstadoEnVivo(mesa, [], ContextoSinReservas()),
            "Posicion actualizada.");
    }

    public async Task<Result> EliminarAsync(int idMesa)
    {
        var mesa = await mesasRepository.ObtenerPorIdAsync(idMesa);
        if (mesa is null)
        {
            return Result.BadRequest("No se encontro la mesa.");
        }

        mesasRepository.Eliminar(mesa);
        await mesasRepository.GuardarCambiosAsync();

        return Result.Ok("Mesa eliminada.");
    }
}
