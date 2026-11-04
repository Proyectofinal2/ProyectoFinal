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
    private async Task<Mesa> MesaConCuentaDeAyerAsync()
    {
        var mesa = Mundo.CrearMesa(capacidad: 20);
        var ayer = Mundo.CrearReserva([mesa], fecha: Mundo.Hoy.AddDays(-1), hora: new TimeOnly(20, 0),
            fechaAtencion: DateTime.Now.AddDays(-1));
        var producto = Mundo.CrearProducto();
        await S.Ventas.AgregarPedidoAsync(ayer.IdReserva, Pedido((producto.IdProducto, 1)), Mundo.IdUsuario);
        return mesa;
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
    public async Task Calendario_hoy_noOfreceHorariosDelTurnoActualConLaUnicaMesaOcupadaPorCuenta()
    {
        var mesa = await MesaConCuentaDeAyerAsync();

        await ConHorarioDeHoyAsync(() => ConSoloLasMesasAsync([mesa.IdMesa], async () =>
        {
            var antes = DateTime.Now;
            var duracion = 90; // valor por defecto de DuracionReservaMinutos en la base de pruebas

            var r = await S.Reservas.ObtenerDisponibilidadAsync(2, Mundo.Hoy);

            Assert.True(r.Success, r.Message);
            Assert.All(r.Data!.HorariosDisponibles,
                hora => Assert.True(Mundo.Hoy.ToDateTime(hora) >= antes.AddMinutes(duracion), $"Se ofrecio {hora} dentro del turno."));
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
