using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Builders;
using MonteCarlo.Tests.Infra;
using static MonteCarlo.Tests.Atajos;

namespace MonteCarlo.Tests.Reservas;

/// <summary>
/// Una mesa con cuenta abierta no se ofrece ni se asigna a otra reserva dentro del turno actual (HU-CON-002 esc. 14),
/// y sin mesa no hay reserva (no existe el estado Pendiente por falta de mesa).
/// Estas pruebas deshabilitan temporalmente las demas mesas de la base compartida y las restauran al terminar.
/// </summary>
public class CuentaAbiertaYDisponibilidadTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private static int _telefono = Random.Shared.Next(1_000_000, 9_000_000);

    /// <summary>Mesa grande con una cuenta abierta de AYER (el turno ya paso y la mesa sigue ocupada).</summary>
    private Task<Mesa> MesaConCuentaDeAyerAsync() => MesaConCuentaSentadaAsync(DateTime.Now.AddDays(-1));

    /// <summary>
    /// Mesa grande con una cuenta abierta cuyo cliente se sento en <paramref name="sentadoEn"/>. La hora de la reserva
    /// se pone a medianoche del dia para que no se solape con los horarios que se prueban.
    /// </summary>
    private async Task<Mesa> MesaConCuentaSentadaAsync(DateTime sentadoEn)
    {
        var mesa = Mundo.CrearMesa(capacidad: 20);
        var reserva = Mundo.CrearReserva([mesa], fecha: DateOnly.FromDateTime(sentadoEn), hora: new TimeOnly(0, 0),
            fechaAtencion: sentadoEn);
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(reserva.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        return mesa;
    }

    /// <summary>Reserva Pendiente (sin mesa) que empieza dentro de <paramref name="minutos"/> minutos.</summary>
    private Reserva ReservaPendienteEn(int minutos)
    {
        var inicio = DateTime.Now.AddMinutes(minutos);
        return Mundo.CrearReserva(estado: "Pendiente", conMesa: false, atendida: false,
            fecha: DateOnly.FromDateTime(inicio), hora: TimeOnly.FromDateTime(inicio));
    }

    /// <summary>Ejecuta la accion con solo esas mesas habilitadas y vuelve a habilitar las demas al terminar.</summary>
    private async Task ConSoloLasMesasAsync(int[] conservar, Func<Task> accion)
    {
        await using var db = Mundo.NuevoContexto();
        var otras = await db.Mesas.Where(m => m.Estado == "Disponible" && !conservar.Contains(m.IdMesa))
            .Select(m => m.IdMesa).ToListAsync();
        await db.Mesas.Where(m => otras.Contains(m.IdMesa)).ExecuteUpdateAsync(s => s.SetProperty(m => m.Estado, "Deshabilitada"));
        try
        {
            await accion();
        }
        finally
        {
            await db.Mesas.Where(m => otras.Contains(m.IdMesa)).ExecuteUpdateAsync(s => s.SetProperty(m => m.Estado, "Disponible"));
        }
    }

    private static CrearReservaRequest Solicitud(DateOnly fecha, TimeOnly hora, int personas, string telefono) => new()
    {
        Nombre = "Prueba", Telefono = telefono, FechaReserva = fecha, HoraReserva = hora, CantidadPersonas = personas
    };

    private static string TelefonoNuevo() => $"7{Interlocked.Increment(ref _telefono):D7}";

    // ---------- Asignar mesa ----------

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task AsignarMesa_aReservaDentroDelTurnoSobreMesaConCuenta_esConflicto()
    {
        var mesa = await MesaConCuentaDeAyerAsync();
        var pendiente = Mundo.CrearReserva(estado: "Pendiente", conMesa: false, atendida: false, hora: Mundo.HoraReciente(1));

        var r = await S.Reservas.AsignarMesaAsync(pendiente.IdReserva, [mesa.IdMesa]);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("cuenta abierta", r.Message);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task AsignarMesa_aReservaPosteriorAlTurno_puedeTomarLaMesaConCuenta()
    {
        var mesa = await MesaConCuentaDeAyerAsync();
        var manana = Mundo.CrearReserva(estado: "Pendiente", conMesa: false, atendida: false,
            fecha: Mundo.Hoy.AddDays(1), hora: new TimeOnly(12, 0));

        var r = await S.Reservas.AsignarMesaAsync(manana.IdReserva, [mesa.IdMesa]);

        Assert.True(r.Success, r.Message);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task AsignarMesa_recienSentado_bloqueaLaMesaHastaLaLiberacionEstimada()
    {
        // Sentados hace 5 min con turno de 90: se estima libre en ~85 min, asi que una reserva en 30 min no la toma.
        var mesa = await MesaConCuentaSentadaAsync(DateTime.Now.AddMinutes(-5));
        var reserva = ReservaPendienteEn(30);

        var r = await S.Reservas.AsignarMesaAsync(reserva.IdReserva, [mesa.IdMesa]);

        Assert.Equal(409, r.StatusCode);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task AsignarMesa_sentadosHaceCasiUnTurno_dejaTomarLaMesaDespuesDeLaLiberacionEstimada()
    {
        // Sentados hace 80 min: se estima libre en ~10 min, asi que una reserva en 30 min si la toma
        // (con la regla anterior, ahora + turno, se habria bloqueado hasta dentro de 90 min).
        var mesa = await MesaConCuentaSentadaAsync(DateTime.Now.AddMinutes(-80));
        var reserva = ReservaPendienteEn(30);

        var r = await S.Reservas.AsignarMesaAsync(reserva.IdReserva, [mesa.IdMesa]);

        Assert.True(r.Success, r.Message);
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task CrearWalkIn_cuentaQueYaPasoElTurno_sigueBloqueandoLaMesa()
    {
        // El walk-in empieza ahora: mientras la cuenta siga abierta la mesa esta ocupada, aunque ya paso su turno.
        var mesa = await MesaConCuentaSentadaAsync(DateTime.Now.AddMinutes(-200));

        var r = await S.Reservas.CrearWalkInAsync(new CrearReservaWalkInRequest { CantidadPersonas = 2, IdsMesa = [mesa.IdMesa] });

        Assert.Equal(409, r.StatusCode);
    }

    // ---------- Crear reserva ----------

    [SqlFact]
    [Trait("HU", "RES-003")]
    public async Task Crear_sinMesaDisponible_devuelve409YNoRegistraAlCliente()
    {
        var telefono = TelefonoNuevo();

        await ConSoloLasMesasAsync([], async () =>
        {
            var r = await S.Reservas.CrearAsync(Solicitud(Mundo.Hoy.AddDays(30), new TimeOnly(13, 0), 2, telefono));

            Assert.Equal(409, r.StatusCode);
        });

        await using var db = Mundo.NuevoContexto();
        Assert.False(await db.Clientes.AnyAsync(c => c.Telefono == telefono));
    }

    [SqlFact]
    [Trait("HU", "RES-003")]
    public async Task Crear_grupoGrande_seConfirmaConMesa_sinUmbralDeRevisionManual()
    {
        var mesa = Mundo.CrearMesa(capacidad: 12);

        await ConSoloLasMesasAsync([mesa.IdMesa], async () =>
        {
            var r = await S.Reservas.CrearAsync(Solicitud(Mundo.Hoy.AddDays(30), new TimeOnly(13, 0), 10, TelefonoNuevo()));

            Assert.True(r.Success, r.Message);
            Assert.Equal("Confirmada", r.Data!.Estado);
            Assert.Equal([mesa.NumeroMesa], r.Data.NumerosMesa);
        });
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task Crear_hoyDentroDelTurnoSobreLaUnicaMesaConCuenta_esConflicto()
    {
        var mesa = await MesaConCuentaDeAyerAsync();

        await ConSoloLasMesasAsync([mesa.IdMesa], async () =>
        {
            var r = await S.Reservas.CrearAsync(Solicitud(Mundo.Hoy, Mundo.HoraReciente(1), 2, TelefonoNuevo()));

            Assert.Equal(409, r.StatusCode);
        });
    }

    // ---------- Calendario ----------

    /// <summary>Horario de hoy de 00:00 a 23:59; se elimina al terminar (la base de pruebas no trae horarios).</summary>
    private async Task ConHorarioDeHoyAsync(Func<Task> accion)
    {
        await using var db = Mundo.NuevoContexto();
        var dia = (byte)DateTime.Now.DayOfWeek;
        var horario = new HorarioOperacion { DiaSemana = dia, HoraApertura = new TimeOnly(0, 0), HoraCierre = new TimeOnly(23, 59), Activo = true };
        db.HorariosOperacion.Add(horario);
        await db.SaveChangesAsync();
        try
        {
            await accion();
        }
        finally
        {
            await db.HorariosOperacion.Where(h => h.IdHorarioOperacion == horario.IdHorarioOperacion).ExecuteDeleteAsync();
        }
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task Calendario_hoy_noOfreceHorariosAntesDeLaLiberacionEstimadaDeLaUnicaMesaConCuenta()
    {
        var sentadoEn = DateTime.Now.AddMinutes(-30);
        var mesa = await MesaConCuentaSentadaAsync(sentadoEn);
        var liberacion = sentadoEn.AddMinutes(90); // DuracionReservaMinutos por defecto en la base de pruebas

        await ConHorarioDeHoyAsync(() => ConSoloLasMesasAsync([mesa.IdMesa], async () =>
        {
            var r = await S.Reservas.ObtenerDisponibilidadAsync(2, Mundo.Hoy);

            Assert.True(r.Success, r.Message);
            Assert.All(r.Data!.HorariosDisponibles,
                hora => Assert.True(Mundo.Hoy.ToDateTime(hora) >= liberacion, $"Se ofrecio {hora} antes de la liberacion estimada."));
        }));
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task Calendario_hoy_cuentaQueYaPasoElTurno_noBloqueaLosHorariosFuturos()
    {
        if (DateTime.Now.Hour >= 22) return;   // sin horarios futuros hoy cerca de la medianoche

        var mesa = await MesaConCuentaSentadaAsync(DateTime.Now.AddMinutes(-200));

        await ConHorarioDeHoyAsync(() => ConSoloLasMesasAsync([mesa.IdMesa], async () =>
        {
            var r = await S.Reservas.ObtenerDisponibilidadAsync(2, Mundo.Hoy);

            Assert.NotEmpty(r.Data!.HorariosDisponibles);
        }));
    }

    [SqlFact]
    [Trait("HU", "CON-002"), Trait("Escenario", "14")]
    public async Task Calendario_manana_sigueOfreciendoLaMesaConCuentaAbiertaHoy()
    {
        var mesa = await MesaConCuentaDeAyerAsync();
        var manana = Mundo.Hoy.AddDays(1);
        var dia = (byte)manana.DayOfWeek;
        await using var db = Mundo.NuevoContexto();
        var horario = new HorarioOperacion { DiaSemana = dia, HoraApertura = new TimeOnly(10, 0), HoraCierre = new TimeOnly(22, 0), Activo = true };
        db.HorariosOperacion.Add(horario);
        await db.SaveChangesAsync();
        try
        {
            await ConSoloLasMesasAsync([mesa.IdMesa], async () =>
            {
                var r = await S.Reservas.ObtenerDisponibilidadAsync(2, manana);

                Assert.True(r.Data!.Disponible, r.Data.MotivoNoDisponible);
            });
        }
        finally
        {
            await db.HorariosOperacion.Where(h => h.IdHorarioOperacion == horario.IdHorarioOperacion).ExecuteDeleteAsync();
        }
    }
}
