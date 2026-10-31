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
    private const string Finalizada = "Finalizada";
    private const string NoShow = "No Show";
    private const string Deshabilitada = "Deshabilitada";

    /// <summary>Telefono del cliente centinela "Walk-in" sembrado en database.sql -- nunca una persona real.</summary>
    private const string TelefonoClienteWalkIn = "00000000";

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

        var umbral = await configuracionService.ObtenerValorUmbralAsync();
        var datosDisponibilidad = await ObtenerDatosDisponibilidadAsync(cantidadPersonas, umbral);
        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var intervaloMinutos = await configuracionService.ObtenerValorIntervaloCalendarioAsync();
        var horasReservadas = datosDisponibilidad.RequiereMesaDisponible
            ? await mesasRepository.ObtenerHorasReservadasPorFechaAsync(fecha)
            : null;

        return Result<DisponibilidadFechaResponse>.Ok(
            CalcularDisponibilidad(fecha, datosDisponibilidad, horasReservadas, DateTime.Now, duracionMinutos, intervaloMinutos),
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
        var umbral = await configuracionService.ObtenerValorUmbralAsync();
        var datosDisponibilidad = await ObtenerDatosDisponibilidadAsync(cantidadPersonas, umbral);
        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var intervaloMinutos = await configuracionService.ObtenerValorIntervaloCalendarioAsync();
        var ahora = DateTime.Now;

        var horasReservadasPorFecha = datosDisponibilidad.RequiereMesaDisponible
            ? await mesasRepository.ObtenerHorasReservadasPorRangoAsync(inicio, fin)
            : null;

        var fechas = new List<DisponibilidadFechaResponse>();
        for (var dia = inicio; dia <= fin; dia = dia.AddDays(1))
        {
            var horasReservadas = horasReservadasPorFecha?.GetValueOrDefault(dia);
            var disponibilidad = CalcularDisponibilidad(dia, datosDisponibilidad, horasReservadas, ahora, duracionMinutos, intervaloMinutos);
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

    // ============================================================
    // HU-REP-001: analisis historico por FechaReserva.
    // ============================================================

    public async Task<Result<List<EstadoReservaReporteResponse>>> ObtenerEstadosReporteAsync()
    {
        var estados = await reservasRepository.ObtenerEstadosReporteAsync();
        return Result<List<EstadoReservaReporteResponse>>.Ok(estados, "Estados de reserva obtenidos exitosamente.");
    }

    public async Task<Result<ReporteReservasResponse>> ObtenerReporteAsync(
        DateOnly? fechaInicio, DateOnly? fechaFin, string? estado)
    {
        if (fechaInicio is null || fechaFin is null)
        {
            return Result<ReporteReservasResponse>.BadRequest("Debes indicar fecha inicial y fecha final.");
        }

        if (fechaInicio > fechaFin)
        {
            return Result<ReporteReservasResponse>.BadRequest("La fecha inicial no puede ser posterior a la fecha final.");
        }

        var estados = await reservasRepository.ObtenerEstadosReporteAsync();
        var estadoNormalizado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();

        if (estadoNormalizado is not null)
        {
            var estadoCatalogo = estados.FirstOrDefault(e =>
                string.Equals(e.Nombre, estadoNormalizado, StringComparison.OrdinalIgnoreCase));

            if (estadoCatalogo is null)
            {
                return Result<ReporteReservasResponse>.BadRequest("El estado de reserva seleccionado no es valido.");
            }

            estadoNormalizado = estadoCatalogo.Nombre;
        }

        var datos = await reservasRepository.ObtenerReporteAsync(fechaInicio.Value, fechaFin.Value, estadoNormalizado);
        var conteos = datos.ConteosPorEstado.ToDictionary(x => x.Estado, x => x.CantidadReservas);
        var estadosMostrados = estadoNormalizado is null
            ? estados
            : estados.Where(e => e.Nombre == estadoNormalizado).ToList();

        var denominadorAsistencia = datos.ReservasAtendidas + datos.NoShows;
        var asistenciaEsAplicable = estadoNormalizado is null && denominadorAsistencia > 0;

        var respuesta = new ReporteReservasResponse
        {
            TotalReservas = datos.TotalReservas,
            PersonasReservadas = datos.PersonasReservadas,
            ReservasAtendidas = datos.ReservasAtendidas,
            NoShows = datos.NoShows,
            Cancelaciones = datos.Cancelaciones,
            PorcentajeAsistencia = asistenciaEsAplicable
                ? Math.Round(datos.ReservasAtendidas * 100m / denominadorAsistencia, 1)
                : null,
            MensajeAsistencia = estadoNormalizado is not null
                ? "No aplicable con filtro de estado."
                : denominadorAsistencia == 0 ? "Sin datos." : null,
            DistribucionPorEstado = estadosMostrados
                .Select(e => new DistribucionEstadoReservaResponse
                {
                    Estado = e.Nombre,
                    CantidadReservas = conteos.GetValueOrDefault(e.Nombre),
                    Porcentaje = datos.TotalReservas == 0
                        ? 0
                        : Math.Round(conteos.GetValueOrDefault(e.Nombre) * 100m / datos.TotalReservas, 1)
                })
                .ToList()
        };

        return Result<ReporteReservasResponse>.Ok(respuesta, "Reporte de reservas obtenido exitosamente.");
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
        reserva.ReservaMesas.Clear();
        reserva.MotivoCancelacion = "Cancelada por el cliente";

        await reservasRepository.GuardarCambiosAsync();

        return Result<ReservaResponse>.Ok(Mapear(reserva), "Tu reserva fue cancelada y la mesa quedo liberada.");
    }

    // ============================================================
    // HU-RES-003: crear reserva
    // ============================================================

    public async Task<Result<ReservaResponse>> CrearAsync(CrearReservaRequest request)
    {
        if (request.CantidadPersonas <= 0)
        {
            return Result<ReservaResponse>.BadRequest("La cantidad de personas debe ser mayor que cero.");
        }

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        if (request.FechaReserva < hoy)
        {
            return Result<ReservaResponse>.BadRequest("No se pueden reservar fechas pasadas.");
        }

        var telefono = request.Telefono.Trim();
        if (telefono == TelefonoClienteWalkIn)
        {
            return Result<ReservaResponse>.BadRequest("El telefono ingresado no es valido.");
        }

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

        var umbral = await configuracionService.ObtenerValorUmbralAsync();
        AsignacionResult resultado;

        if (request.CantidadPersonas > umbral)
        {
            resultado = new AsignacionResult([], RequiereManual: true);
        }
        else
        {
            var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
            var datosMesas = await CargarMesasConReservasDelDiaAsync(request.FechaReserva);
            resultado = BuscarMejorAsignacion(request.CantidadPersonas, request.HoraReserva, duracionMinutos, datosMesas);
        }

        var requiereAsignacionManual = resultado.RequiereManual;
        var nombreEstado = resultado.RequiereManual ? Pendiente : Confirmada;
        var estado = await reservasRepository.ObtenerEstadoAsync(nombreEstado);

        if (estado is null)
        {
            return Result<ReservaResponse>.InternalError($"El estado \"{nombreEstado}\" no esta configurado.");
        }

        var codigo = await GenerarCodigoUnicoAsync();

        var reserva = new Reserva
        {
            CodigoReserva = codigo,
            IdCliente = cliente.IdCliente,
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

        foreach (var mesa in resultado.Mesas)
        {
            reserva.ReservaMesas.Add(new ReservaMesa { IdMesa = mesa.IdMesa });
        }

        reservasRepository.Agregar(reserva);
        await reservasRepository.GuardarCambiosAsync();

        var creada = await reservasRepository.ObtenerPorCodigoAsync(codigo);

        return Result<ReservaResponse>.Ok(
            Mapear(creada!),
            resultado.RequiereManual
                ? "Reserva registrada. Requiere revision administrativa."
                : "Reserva confirmada exitosamente.");
    }

    // ============================================================
    // Asignación de mesas (HU-MES-005/006 / HU-OPE-004)
    // ============================================================

    public async Task<Result<AsignacionSugeridaResponse>> SugerirAsignacionPorReservaAsync(int idReserva)
    {
        var reserva = await reservasRepository.ObtenerPorIdAsync(idReserva);
        if (reserva is null) return Result<AsignacionSugeridaResponse>.NotFound("No se encontro la reserva.");

        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var datosMesas = await CargarMesasConReservasDelDiaAsync(reserva.FechaReserva);
        ExcluirReservaPropia(datosMesas.HorasReservadas, reserva);

        var resultado = BuscarMejorAsignacion(reserva.CantidadPersonas, reserva.HoraReserva, duracionMinutos, datosMesas);

        return Result<AsignacionSugeridaResponse>.Ok(new AsignacionSugeridaResponse
        {
            NumerosMesa = resultado.Mesas.Select(m => m.NumeroMesa).ToList(),
            EsCombinacionDeMesas = resultado.EsCombinacionDeMesas,
            RequiereRevisionManual = resultado.RequiereManual
        }, "Sugerencia calculada.");
    }

    public async Task<Result<AsignacionMesaResponse>> AsignarMesaAsync(int idReserva, List<int> idsMesa)
    {
        if (idsMesa is not { Count: 1 or 2 }) return Result<AsignacionMesaResponse>.BadRequest("Debes indicar 1 o 2 mesas.");

        var reserva = await reservasRepository.ObtenerPorIdAsync(idReserva);
        if (reserva is null) return Result<AsignacionMesaResponse>.NotFound("No se encontro la reserva.");

        if (reserva.EstadoReserva.Nombre is not (Pendiente or Confirmada))
            return Result<AsignacionMesaResponse>.Conflict($"La reserva esta en estado \"{reserva.EstadoReserva.Nombre}\" y no admite asignacion de mesa.");

        if (reserva.EstadoReserva.Nombre == Confirmada && reserva.FechaAtencion is not null)
            return Result<AsignacionMesaResponse>.Conflict("El cliente ya esta sentado, no se puede reasignar la mesa.");

        var mesas = await mesasRepository.ObtenerPorIdsAsync(idsMesa);
        if (mesas.Count != idsMesa.Count) return Result<AsignacionMesaResponse>.BadRequest("Una o mas mesas indicadas no existen.");

        var mesaDeshabilitada = mesas.FirstOrDefault(m => m.Estado == Deshabilitada);
        if (mesaDeshabilitada is not null) return Result<AsignacionMesaResponse>.BadRequest($"La mesa {mesaDeshabilitada.NumeroMesa} esta deshabilitada.");

        if (mesas.Sum(m => m.Capacidad) < reserva.CantidadPersonas)
            return Result<AsignacionMesaResponse>.BadRequest("La capacidad combinada de las mesas elegidas no alcanza para la cantidad de personas.");

        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var horasReservadas = await mesasRepository.ObtenerHorasReservadasPorFechaAsync(reserva.FechaReserva);
        ExcluirReservaPropia(horasReservadas, reserva);

        var mesaOcupada = mesas.FirstOrDefault(m => !EstaLibre(m, horasReservadas, reserva.HoraReserva, duracionMinutos));
        if (mesaOcupada is not null) return Result<AsignacionMesaResponse>.Conflict($"La mesa {mesaOcupada.NumeroMesa} ya tiene otra reserva activa que se solapa con este horario.");

        var estadoConfirmada = await reservasRepository.ObtenerEstadoAsync(Confirmada);
        if (estadoConfirmada is null) return Result<AsignacionMesaResponse>.InternalError("El estado \"Confirmada\" no esta configurado.");

        reserva.ReservaMesas.Clear();
        foreach (var mesa in mesas) reserva.ReservaMesas.Add(new ReservaMesa { IdMesa = mesa.IdMesa });

        reserva.EstadoReserva = estadoConfirmada;
        reserva.RequiereAsignacionManual = false;

        await reservasRepository.GuardarCambiosAsync();

        return Result<AsignacionMesaResponse>.Ok(new AsignacionMesaResponse
        {
            NumerosMesa = mesas.Select(m => m.NumeroMesa).ToList(),
            EsCombinacionDeMesas = mesas.Count > 1,
            Estado = estadoConfirmada.Nombre
        }, "Mesa asignada correctamente.");
    }

    public async Task<Result<AsignacionSugeridaResponse>> SugerirWalkInAsync(int cantidadPersonas)
    {
        if (cantidadPersonas <= 0) return Result<AsignacionSugeridaResponse>.BadRequest("La cantidad de personas debe ser mayor que cero.");

        var ahora = DateTime.Now;
        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var datosMesas = await CargarMesasConReservasDelDiaAsync(DateOnly.FromDateTime(ahora));
        var resultado = BuscarMejorAsignacion(cantidadPersonas, TimeOnly.FromDateTime(ahora), duracionMinutos, datosMesas);

        return Result<AsignacionSugeridaResponse>.Ok(new AsignacionSugeridaResponse
        {
            NumerosMesa = resultado.Mesas.Select(m => m.NumeroMesa).ToList(),
            EsCombinacionDeMesas = resultado.EsCombinacionDeMesas,
            RequiereRevisionManual = resultado.RequiereManual
        }, "Sugerencia calculada.");
    }

    public async Task<Result<ReservaWalkInResponse>> CrearWalkInAsync(CrearReservaWalkInRequest request)
    {
        if (request.CantidadPersonas <= 0) return Result<ReservaWalkInResponse>.BadRequest("La cantidad de personas debe ser mayor que cero.");
        if (request.IdsMesa is not { Count: 1 or 2 }) return Result<ReservaWalkInResponse>.BadRequest("Debes indicar 1 o 2 mesas.");

        var clienteWalkIn = await clientesRepository.ObtenerPorTelefonoAsync(TelefonoClienteWalkIn);
        if (clienteWalkIn is null) return Result<ReservaWalkInResponse>.InternalError("El cliente centinela \"Walk-in\" no esta sembrado en la base de datos.");

        var mesas = await mesasRepository.ObtenerPorIdsAsync(request.IdsMesa);
        if (mesas.Count != request.IdsMesa.Count) return Result<ReservaWalkInResponse>.BadRequest("Una o mas mesas indicadas no existen.");

        var mesaDeshabilitada = mesas.FirstOrDefault(m => m.Estado == Deshabilitada);
        if (mesaDeshabilitada is not null) return Result<ReservaWalkInResponse>.BadRequest($"La mesa {mesaDeshabilitada.NumeroMesa} esta deshabilitada.");

        if (mesas.Sum(m => m.Capacidad) < request.CantidadPersonas) return Result<ReservaWalkInResponse>.BadRequest("La capacidad combinada de las mesas elegidas no alcanza para la cantidad de personas.");

        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);
        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var horasReservadas = await mesasRepository.ObtenerHorasReservadasPorFechaAsync(hoy);

        var mesaOcupada = mesas.FirstOrDefault(m => !EstaLibre(m, horasReservadas, horaActual, duracionMinutos));
        if (mesaOcupada is not null) return Result<ReservaWalkInResponse>.Conflict($"La mesa {mesaOcupada.NumeroMesa} ya tiene otra reserva activa que se solapa con este horario.");

        var estadoConfirmada = await reservasRepository.ObtenerEstadoAsync(Confirmada);
        if (estadoConfirmada is null) return Result<ReservaWalkInResponse>.InternalError("El estado \"Confirmada\" no esta configurado.");

        var reserva = new Reserva
        {
            IdCliente = clienteWalkIn.IdCliente,
            IdEstadoReserva = estadoConfirmada.IdEstadoReserva,
            FechaReserva = hoy,
            HoraReserva = horaActual,
            CantidadPersonas = request.CantidadPersonas,
            EsWalkIn = true,
            FechaAtencion = ahora,
            FechaCreacion = ahora
        };

        foreach (var mesa in mesas) reserva.ReservaMesas.Add(new ReservaMesa { IdMesa = mesa.IdMesa });

        reservasRepository.Agregar(reserva);
        await reservasRepository.GuardarCambiosAsync();

        return Result<ReservaWalkInResponse>.Ok(new ReservaWalkInResponse
        {
            IdReserva = reserva.IdReserva,
            NumerosMesa = mesas.Select(m => m.NumeroMesa).ToList(),
            EsCombinacionDeMesas = mesas.Count > 1
        }, "Cliente sentado correctamente.");
    }

    public async Task<Result> LiberarMesaAsync(int idMesa)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var reserva = await reservasRepository.ObtenerConfirmadaActivaPorMesaAsync(idMesa, hoy);
        if (reserva is null) return Result.BadRequest("Esa mesa no tiene ninguna reserva activa que liberar.");

        var estadoFinalizada = await reservasRepository.ObtenerEstadoAsync(Finalizada);
        if (estadoFinalizada is null) return Result.InternalError("El estado \"Finalizada\" no esta configurado.");

        reserva.EstadoReserva = estadoFinalizada;
        reserva.ReservaMesas.Clear();
        reserva.MotivoCancelacion = "Liberada sin consumo";

        await reservasRepository.GuardarCambiosAsync();

        return Result.Ok("Mesa liberada.");
    }

    // ============================================================
    // HU-OPE-003: registrar atencion y no show
    // ============================================================

    public async Task<Result> RegistrarAtencionAsync(string codigo)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());
        if (reserva is null) return Result.BadRequest("No se encontro la reserva.");

        if (reserva.EstadoReserva.Nombre != Confirmada)
            return Result.BadRequest($"Solo se puede registrar atencion a reservas Confirmadas. Estado actual: {reserva.EstadoReserva.Nombre}.");

        if (reserva.FechaAtencion is not null)
            return Result.BadRequest("El cliente ya fue registrado como atendido.");

        // Registrar la hora exacta de llegada, el estado se mantiene en Confirmada.
        reserva.FechaAtencion = DateTime.Now;

        await reservasRepository.GuardarCambiosAsync();

        return Result.Ok("Llegada registrada correctamente.");
    }

    public async Task<Result> RegistrarNoShowAsync(string codigo)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());
        if (reserva is null) return Result.BadRequest("No se encontro la reserva.");

        if (reserva.EstadoReserva.Nombre != Confirmada)
            return Result.BadRequest($"Solo se puede marcar No Show a reservas Confirmadas. Estado actual: {reserva.EstadoReserva.Nombre}.");

        var estadoNoShow = await reservasRepository.ObtenerEstadoAsync("No Show");
        if (estadoNoShow is null)
            return Result.InternalError("El estado \"No Show\" no esta configurado en el sistema.");

        // Actualizar estado y liberar mesas asignadas
        reserva.EstadoReserva = estadoNoShow;
        reserva.ReservaMesas.Clear();
        reserva.MotivoCancelacion = "Marcada como No Show por el administrador";

        await reservasRepository.GuardarCambiosAsync();

        return Result.Ok("Reserva marcada como No Show y mesa liberada.");
    }

    // ============================================================
    // Reservas del dia y Búsqueda (HU-OPE-005)
    // ============================================================

    public async Task<Result<List<ReservaDelDiaResponse>>> ObtenerDelDiaAsync(DateOnly fecha)
    {
        var reservas = await reservasRepository.ObtenerPorFechaAsync(fecha);

        var respuesta = reservas
           // .Where(r => r.EstadoReserva.Nombre != Cancelada)
            .Select(MapearDelDia)
            .ToList();

        return Result<List<ReservaDelDiaResponse>>.Ok(respuesta, "Reservas del dia obtenidas exitosamente.");
    }

    public async Task<Result<List<ReservaDelDiaResponse>>> BuscarAsync(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return Result<List<ReservaDelDiaResponse>>.BadRequest("Ingrese un termino de busqueda.");

        const int limiteResultados = 50;
        var reservas = await reservasRepository.BuscarPorNombreOTelefonoAsync(termino, limiteResultados);

        var respuesta = reservas.Select(MapearDelDia).ToList();

        return Result<List<ReservaDelDiaResponse>>.Ok(respuesta, $"{respuesta.Count} reserva(s) encontrada(s).");
    }

    // ============================================================
    // Métodos auxiliares privados
    // ============================================================

    private static void ExcluirReservaPropia(Dictionary<int, List<TimeOnly>> horasReservadas, Reserva reserva)
    {
        foreach (var reservaMesa in reserva.ReservaMesas)
        {
            if (horasReservadas.TryGetValue(reservaMesa.IdMesa, out var horas))
            {
                horas.Remove(reserva.HoraReserva);
            }
        }
    }

    private async Task<DatosMesasDelDia> CargarMesasConReservasDelDiaAsync(DateOnly fecha)
    {
        var mesas = await mesasRepository.ObtenerMesasHabilitadasAsync();
        var horasReservadas = await mesasRepository.ObtenerHorasReservadasPorFechaAsync(fecha);
        return new DatosMesasDelDia(mesas, horasReservadas);
    }

    private static AsignacionResult BuscarMejorAsignacion(int cantidadPersonas, TimeOnly hora, int duracionMinutos, DatosMesasDelDia datosMesas)
    {
        var mesasLibres = FiltrarMesasLibres(datosMesas.Mesas, datosMesas.HorasReservadas, hora, duracionMinutos);
        var individual = BuscarMesaIndividual(mesasLibres, cantidadPersonas);

        if (individual is not null) return new AsignacionResult([individual], RequiereManual: false);

        var mejorPar = BuscarMejorParAdyacente(mesasLibres, cantidadPersonas);

        return mejorPar is not null ? new AsignacionResult(mejorPar, RequiereManual: false) : new AsignacionResult([], RequiereManual: true);
    }

    private static List<Mesa>? BuscarMejorParAdyacente(List<Mesa> mesasLibres, int cantidadPersonas)
    {
        List<Mesa>? mejorPar = null;
        var mejorCapacidadCombinada = int.MaxValue;

        for (var i = 0; i < mesasLibres.Count; i++)
        {
            for (var j = i + 1; j < mesasLibres.Count; j++)
            {
                var mesaA = mesasLibres[i];
                var mesaB = mesasLibres[j];
                var capacidadCombinada = mesaA.Capacidad + mesaB.Capacidad;

                if (capacidadCombinada < cantidadPersonas || capacidadCombinada >= mejorCapacidadCombinada) continue;
                if (!MesaGeometryHelper.SonAdyacentes(mesaA, mesaB)) continue;

                mejorPar = [mesaA, mesaB];
                mejorCapacidadCombinada = capacidadCombinada;
            }
        }
        return mejorPar;
    }

    private sealed record DatosMesasDelDia(List<Mesa> Mesas, Dictionary<int, List<TimeOnly>> HorasReservadas);

    private static bool EstaLibre(Mesa mesa, Dictionary<int, List<TimeOnly>> horasReservadas, TimeOnly hora, int duracionMinutos) =>
        !horasReservadas.TryGetValue(mesa.IdMesa, out var horas) || !horas.Any(h => SeSolapan(hora, h, duracionMinutos));

    private static List<Mesa> FiltrarMesasLibres(List<Mesa> mesas, Dictionary<int, List<TimeOnly>> horasReservadas, TimeOnly hora, int duracionMinutos) =>
        mesas.Where(m => EstaLibre(m, horasReservadas, hora, duracionMinutos)).ToList();

    private static Mesa? BuscarMesaIndividual(List<Mesa> mesasLibres, int cantidadPersonas) =>
        mesasLibres.Where(m => m.Capacidad >= cantidadPersonas).OrderBy(m => m.Capacidad).FirstOrDefault();

    private static bool SeSolapan(TimeOnly horaNueva, TimeOnly horaExistente, int duracionMinutos)
    {
        var diferencia = Math.Abs((horaNueva.ToTimeSpan() - horaExistente.ToTimeSpan()).TotalMinutes);
        return diferencia < duracionMinutos;
    }

    private async Task<string> GenerarCodigoUnicoAsync()
    {
        for (var intento = 0; intento < 10; intento++)
        {
            var aleatorio = new char[LongitudAleatoria];
            for (var i = 0; i < LongitudAleatoria; i++) aleatorio[i] = AlfabetoCodigo[Random.Shared.Next(AlfabetoCodigo.Length)];

            var codigo = PrefijoCodigo + new string(aleatorio);

            if (!await reservasRepository.ExisteCodigoAsync(codigo)) return codigo;
        }
        throw new InvalidOperationException("No se pudo generar un codigo unico despues de 10 intentos.");
    }

    private async Task<DatosDisponibilidad> ObtenerDatosDisponibilidadAsync(int cantidadPersonas, int umbral)
    {
        var cierres = await reservasRepository.ObtenerDiasCierreFijoAsync();
        var cierresEventuales = await reservasRepository.ObtenerFechasCierreEventualAsync(DateOnly.FromDateTime(DateTime.Now));
        var horarios = await reservasRepository.ObtenerHorariosOperacionAsync();
        var requiereMesaDisponible = cantidadPersonas <= umbral;
        var mesasHabilitadas = requiereMesaDisponible ? await mesasRepository.ObtenerMesasHabilitadasAsync() : [];

        return new DatosDisponibilidad(cantidadPersonas, cierres.ToHashSet(), cierresEventuales.ToHashSet(), horarios, mesasHabilitadas, requiereMesaDisponible);
    }

    private static DisponibilidadFechaResponse CalcularDisponibilidad(DateOnly fecha, DatosDisponibilidad datosDisponibilidad, Dictionary<int, List<TimeOnly>>? horasReservadas, DateTime ahora, int duracionMinutos, int intervaloMinutos)
    {
        var respuesta = new DisponibilidadFechaResponse { Fecha = fecha };
        var hoy = DateOnly.FromDateTime(ahora);

        if (fecha < hoy) { respuesta.MotivoNoDisponible = "No se pueden seleccionar fechas pasadas."; return respuesta; }
        var diaSemana = (byte)fecha.DayOfWeek;
        if (datosDisponibilidad.CierresFijos.Contains(diaSemana)) { respuesta.MotivoNoDisponible = "El restaurante permanece cerrado este dia."; return respuesta; }
        if (datosDisponibilidad.CierresEventuales.Contains(fecha)) { respuesta.MotivoNoDisponible = "El restaurante permanecera cerrado en esta fecha."; return respuesta; }

        var horario = datosDisponibilidad.Horarios.FirstOrDefault(h => h.DiaSemana == diaSemana && h.Activo);
        if (horario is null) { respuesta.MotivoNoDisponible = "No hay horario de operacion configurado para este dia."; return respuesta; }
        if (datosDisponibilidad.MesasCandidatas.Count == 0 && datosDisponibilidad.RequiereMesaDisponible) { respuesta.MotivoNoDisponible = "No hay mesas habilitadas."; return respuesta; }

        var horaActual = TimeOnly.FromDateTime(ahora);
        var duracionReserva = TimeSpan.FromMinutes(duracionMinutos);
        var intervaloLlegada = TimeSpan.FromMinutes(intervaloMinutos);
        var datosMesas = new DatosMesasDelDia(datosDisponibilidad.MesasCandidatas, horasReservadas ?? []);

        for (var hora = horario.HoraApertura; hora.ToTimeSpan() + duracionReserva <= horario.HoraCierre.ToTimeSpan(); hora = hora.Add(intervaloLlegada))
        {
            if (fecha == hoy && hora <= horaActual) continue;
            if (datosDisponibilidad.RequiereMesaDisponible && BuscarMejorAsignacion(datosDisponibilidad.CantidadPersonas, hora, duracionMinutos, datosMesas).RequiereManual) continue;

            respuesta.HorariosDisponibles.Add(hora);
        }

        respuesta.Disponible = respuesta.HorariosDisponibles.Count > 0;
        if (!respuesta.Disponible) respuesta.MotivoNoDisponible = "No hay horarios disponibles para esta fecha.";

        return respuesta;
    }

    private static bool PuedeCancelarse(Reserva reserva) => reserva.EstadoReserva.Nombre is Pendiente or Confirmada;

    private static ReservaResponse Mapear(Reserva reserva) => new()
    {
        CodigoReserva = reserva.CodigoReserva!,
        NombreCliente = $"{reserva.Cliente.Nombre} {reserva.Cliente.Apellido}".Trim(),
        FechaReserva = reserva.FechaReserva,
        HoraReserva = reserva.HoraReserva,
        CantidadPersonas = reserva.CantidadPersonas,
        Estado = reserva.EstadoReserva.Nombre,
        PuedeCancelarse = PuedeCancelarse(reserva),
        NumerosMesa = reserva.ReservaMesas.Select(rm => rm.Mesa.NumeroMesa).ToList()
    };

    private static ReservaDelDiaResponse MapearDelDia(Reserva r) => new()
    {
        IdReserva = r.IdReserva,
        CodigoReserva = r.CodigoReserva,
        FechaReserva = r.FechaReserva,
        HoraReserva = r.HoraReserva,
        NombreCliente = $"{r.Cliente.Nombre} {r.Cliente.Apellido}".Trim(),
        Telefono = r.EsWalkIn ? null : r.Cliente.Telefono,
        CantidadPersonas = r.CantidadPersonas,
        Estado = r.EstadoReserva.Nombre,
        NumerosMesa = r.ReservaMesas.Select(rm => rm.Mesa.NumeroMesa).OrderBy(n => n).ToList(),
        Observaciones = r.Observaciones,
        EsWalkIn = r.EsWalkIn,
        FechaAtencion = r.FechaAtencion // <-- AGREGADO
    };


    // ============================================================
    // HU-OPE-002: Gestionar estados manualmente
    // ============================================================

    public async Task<Result> CambiarEstadoAsync(string codigo, string nuevoEstado)
    {
        var reserva = await reservasRepository.ObtenerPorCodigoAsync(codigo.Trim());
        if (reserva is null) return Result.BadRequest("No se encontró la reserva.");

        var estadoActual = reserva.EstadoReserva.Nombre;

        // Escenario 3: No se permite pasar a Finalizada manualmente
        if (nuevoEstado.Equals("Finalizada", StringComparison.OrdinalIgnoreCase))
        {
            return Result.BadRequest("La reserva debe cerrarse mediante el registro de consumo y pago (HU-CON-002).");
        }

        // Escenario 2: Validar transiciones permitidas
        bool transicionValida = false;

        if (estadoActual == Pendiente && (nuevoEstado == Confirmada || nuevoEstado == Cancelada))
            transicionValida = true;
        else if (estadoActual == Confirmada && nuevoEstado == Cancelada)
            transicionValida = true;
        // Cancelada, No Show y Finalizada no admiten cambios manuales

        if (!transicionValida)
        {
            return Result.BadRequest($"Transición no permitida de '{estadoActual}' a '{nuevoEstado}'.");
        }

        var estadoDestino = await reservasRepository.ObtenerEstadoAsync(nuevoEstado);
        if (estadoDestino is null) return Result.InternalError($"El estado '{nuevoEstado}' no existe.");

        // Escenario 1: Actualizar estado y registrar el cambio
        reserva.EstadoReserva = estadoDestino;

        if (nuevoEstado == Cancelada)
        {
            reserva.ReservaMesas.Clear();
            reserva.MotivoCancelacion = "Cancelada por el administrador";
        }

        await reservasRepository.GuardarCambiosAsync();

        return Result.Ok($"Estado cambiado a {nuevoEstado} exitosamente.");
    }

    private sealed record DatosDisponibilidad(int CantidadPersonas, HashSet<byte> CierresFijos, HashSet<DateOnly> CierresEventuales, List<HorarioOperacion> Horarios, List<Mesa> MesasCandidatas, bool RequiereMesaDisponible);
}
