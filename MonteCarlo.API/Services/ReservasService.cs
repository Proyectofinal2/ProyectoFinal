using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class ReservasService(
    IReservasRepository reservasRepository,
    IMesasRepository mesasRepository,
    IClientesRepository clientesRepository,
    IConfiguracionService configuracionService) : IReservasService
{
    private const string Pendiente = "Pendiente";
    private const string Confirmada = "Confirmada";
    private const string Cancelada = "Cancelada";

    private static readonly TimeSpan IntervaloLlegada = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DuracionReserva = TimeSpan.FromMinutes(90);

    // Alfabeto sin vocales para evitar palabras ofensivas accidentales.
    private const string AlfabetoCodigo = "BCDFGHJKLMNPQRSTVWXYZ23456789";
    private const int LongitudAleatoria = 5;
    private const string PrefijoCodigo = "MC-";

    // ============================================================
    // HU-RES-002 / HU-RES-005: disponibilidad y consulta
    // ============================================================

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

    // ============================================================
    // HU-RES-003: crear reserva
    // ============================================================

    public async Task<Result<ReservaResponse>> CrearAsync(CrearReservaRequest request)
    {
        // ---------- Validaciones básicas ----------
        if (request.CantidadPersonas <= 0)
        {
            return Result<ReservaResponse>.BadRequest("La cantidad de personas debe ser mayor que cero.");
        }

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        if (request.FechaReserva < hoy)
        {
            return Result<ReservaResponse>.BadRequest("No se pueden reservar fechas pasadas.");
        }

        // ---------- Buscar o crear el cliente ----------
        var telefono = request.Telefono.Trim();
        var cliente = await clientesRepository.ObtenerPorTelefonoAsync(telefono);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                Nombre = request.Nombre.Trim(),
                Apellido = string.IsNullOrWhiteSpace(request.Apellido) ? null : request.Apellido.Trim(),
                Telefono = telefono,
                CorreoElectronico = string.IsNullOrWhiteSpace(request.CorreoElectronico)
                    ? null
                    : request.CorreoElectronico.Trim(),
                FechaCreacion = DateTime.Now
            };
            clientesRepository.Agregar(cliente);
            await clientesRepository.GuardarCambiosAsync();
        }

        // ---------- Determinar el estado según el umbral (RN-01 / RN-02) ----------
        var umbral = await configuracionService.ObtenerValorUmbralAsync();
        var dentroDelUmbral = request.CantidadPersonas <= umbral;

        var nombreEstado = dentroDelUmbral ? Confirmada : Pendiente;
        var estado = await reservasRepository.ObtenerEstadoAsync(nombreEstado);

        if (estado is null)
        {
            return Result<ReservaResponse>.InternalError($"El estado \"{nombreEstado}\" no esta configurado.");
        }

        // ---------- Asignar mesa si aplica (RN-01 y RN-03) ----------
        Mesa? mesaAsignada = null;
        var requiereAsignacionManual = false;

        if (dentroDelUmbral)
        {
            mesaAsignada = await BuscarMesaDisponibleAsync(request.CantidadPersonas, request.FechaReserva, request.HoraReserva);

            // Si no hay mesa, la reserva queda Confirmada pero marcada para asignación manual.
            if (mesaAsignada is null)
            {
                requiereAsignacionManual = true;
            }
        }
        else
        {
            // Supera el umbral: se marca para revisión administrativa (RN-02).
            requiereAsignacionManual = true;
        }

        // ---------- Generar el código único ----------
        var codigo = await GenerarCodigoUnicoAsync();

        // ---------- Crear la reserva ----------
        var reserva = new Reserva
        {
            CodigoReserva = codigo,
            IdCliente = cliente.IdCliente,
            IdMesa = mesaAsignada?.IdMesa,
            IdEstadoReserva = estado.IdEstadoReserva,
            FechaReserva = request.FechaReserva,
            HoraReserva = request.HoraReserva,
            CantidadPersonas = request.CantidadPersonas,
            RequiereAsignacionManual = requiereAsignacionManual,
            Observaciones = string.IsNullOrWhiteSpace(request.Observaciones)
                ? null
                : request.Observaciones.Trim(),
            FechaCreacion = DateTime.Now
        };

        reservasRepository.Agregar(reserva);
        await reservasRepository.GuardarCambiosAsync();

        // ---------- Recargar con las relaciones para el mapeo ----------
        var creada = await reservasRepository.ObtenerPorCodigoAsync(codigo);

        return Result<ReservaResponse>.Ok(
            Mapear(creada!),
            dentroDelUmbral
                ? "Reserva confirmada exitosamente."
                : "Reserva registrada. Requiere revision administrativa.");
    }

    // ============================================================
    // Métodos auxiliares
    // ============================================================

    /// <summary>
    /// RN-01 / RN-03: devuelve la mesa de menor capacidad que quepa al grupo
    /// y que no tenga otra reserva activa en la franja de 90 minutos.
    /// </summary>
    private async Task<Mesa?> BuscarMesaDisponibleAsync(int cantidadPersonas, DateOnly fecha, TimeOnly hora)
    {
        var candidatas = await mesasRepository.ObtenerDisponiblesPorCapacidadAsync(cantidadPersonas);

        foreach (var mesa in candidatas)
        {
            var reservasDelDia = await mesasRepository.ObtenerReservasActivasPorMesaYFechaAsync(mesa.IdMesa, fecha);

            var haySolapamiento = reservasDelDia.Any(r =>
                SeSolapan(hora, r.HoraReserva));

            if (!haySolapamiento)
            {
                return mesa;
            }
        }

        return null;
    }

    /// <summary>
    /// Dos reservas se solapan si la diferencia entre sus horas de inicio
    /// es menor que la duración total de una reserva (90 minutos).
    /// </summary>
    private static bool SeSolapan(TimeOnly horaNueva, TimeOnly horaExistente)
    {
        var diferencia = Math.Abs((horaNueva.ToTimeSpan() - horaExistente.ToTimeSpan()).TotalMinutes);
        return diferencia < DuracionReserva.TotalMinutes;
    }

    /// <summary>
    /// Genera un código único alfanumérico con el formato MC-XXXXX.
    /// Reintenta hasta 10 veces si el código ya existe.
    /// </summary>
    private async Task<string> GenerarCodigoUnicoAsync()
    {
        for (var intento = 0; intento < 10; intento++)
        {
            var aleatorio = new char[LongitudAleatoria];
            for (var i = 0; i < LongitudAleatoria; i++)
            {
                aleatorio[i] = AlfabetoCodigo[Random.Shared.Next(AlfabetoCodigo.Length)];
            }

            var codigo = PrefijoCodigo + new string(aleatorio);

            if (!await reservasRepository.ExisteCodigoAsync(codigo))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("No se pudo generar un codigo unico despues de 10 intentos.");
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