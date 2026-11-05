using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MonteCarlo.API.Data;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories;
using MonteCarlo.API.Services;
using MonteCarlo.API.Services.Interfaces;
using MonteCarlo.Tests.Infra;
using NSubstitute;

namespace MonteCarlo.Tests.Builders;

/// <summary>
/// Servicios reales sobre un DbContext propio (una "sesion" de la API). Para simular dos peticiones simultaneas se
/// crean dos instancias: cada una tiene su contexto, como dos scopes de ASP.NET.
/// </summary>
public sealed class Sesion : IDisposable
{
    public MonteCarloDbContext Db { get; }
    public VentasService Ventas { get; }
    public MesasService Mesas { get; }
    public ReservasService Reservas { get; }

    public Sesion(SqlServerFixture fixture)
    {
        Db = fixture.NuevoContexto();
        var ventasRepo = new VentasRepository(Db);
        var reservasRepo = new ReservasRepository(Db);
        var mesasRepo = new MesasRepository(Db);
        var configuracion = new ConfiguracionService(new ConfiguracionRepository(Db));

        Ventas = new VentasService(ventasRepo, reservasRepo, new ProductosRepository(Db), new MetodosPagoRepository(Db),
            configuracion, NullLogger<VentasService>.Instance);
        Mesas = new MesasService(mesasRepo, reservasRepo, ventasRepo, configuracion);
        Reservas = new ReservasService(reservasRepo, mesasRepo, new ClientesRepository(Db), configuracion, ventasRepo,
            Substitute.For<INotificacionesService>());
    }

    public void Dispose() => Db.Dispose();
}

/// <summary>
/// Datos de una prueba. Cada prueba crea sus propias mesas, productos y reservas (nombres y numeros unicos), de modo
/// que no depende del orden ni de lo que dejaron otras pruebas en la base temporal.
/// </summary>
public sealed class Mundo : IDisposable
{
    private static int _siguiente = Random.Shared.Next(100_000, 400_000);

    private readonly SqlServerFixture _fixture;
    private readonly Sesion _sesion;

    public Mundo(SqlServerFixture fixture)
    {
        _fixture = fixture;
        _sesion = new Sesion(fixture);
        using var db = fixture.NuevoContexto();
        IdUsuario = db.Usuarios.AsNoTracking().OrderBy(u => u.IdUsuario).Select(u => u.IdUsuario).First();
        IdMetodoPago = db.MetodosPago.AsNoTracking().OrderBy(m => m.IdMetodoPago).Select(m => m.IdMetodoPago).First();
    }

    public int IdUsuario { get; }
    public int IdMetodoPago { get; }

    /// <summary>Sesion principal; sirve para consultar y para llamar a los servicios.</summary>
    public Sesion Servicios => _sesion;
    public Sesion NuevaSesion() => new(_fixture);
    public MonteCarloDbContext NuevoContexto() => _fixture.NuevoContexto();

    private static int Siguiente() => Interlocked.Increment(ref _siguiente);

    // ---------- Catalogo ----------

    public CategoriaProducto CrearCategoria(bool activa = true)
    {
        using var db = NuevoContexto();
        var n = Siguiente();
        var categoria = new CategoriaProducto { Nombre = $"Cat {n}", Orden = 1, Activa = activa, FechaCreacion = DateTime.Now };
        db.CategoriasProducto.Add(categoria);
        db.SaveChanges();
        return categoria;
    }

    public Producto CrearProducto(decimal precio = 1000m, bool disponible = true, int? idCategoria = null)
    {
        var categoria = idCategoria ?? CrearCategoria().IdCategoria;
        using var db = NuevoContexto();
        var n = Siguiente();
        var producto = new Producto
        {
            Nombre = $"Prod {n}", IdCategoria = categoria, Precio = precio, Disponible = disponible, FechaCreacion = DateTime.Now
        };
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }

    public void CambiarPrecio(int idProducto, decimal precio)
    {
        using var db = NuevoContexto();
        db.Productos.Where(p => p.IdProducto == idProducto).ExecuteUpdate(s => s.SetProperty(p => p.Precio, precio));
    }

    public void CambiarCategoriaActiva(int idCategoria, bool activa)
    {
        using var db = NuevoContexto();
        db.CategoriasProducto.Where(c => c.IdCategoria == idCategoria).ExecuteUpdate(s => s.SetProperty(c => c.Activa, activa));
    }

    // ---------- Mesas y reservas ----------

    public Mesa CrearMesa(int capacidad = 4, string estado = "Disponible")
    {
        using var db = NuevoContexto();
        var mesa = new Mesa { NumeroMesa = Siguiente(), Capacidad = capacidad, Estado = estado };
        db.Mesas.Add(mesa);
        db.SaveChanges();
        return mesa;
    }

    public static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Hora de hoy "hace unos minutos"; nunca cruza la medianoche.</summary>
    public static TimeOnly HoraReciente(int minutosAtras = 10)
    {
        var ahora = DateTime.Now;
        var hora = ahora.AddMinutes(-minutosAtras);
        return hora.Date == ahora.Date ? TimeOnly.FromDateTime(hora) : new TimeOnly(0, 0);
    }

    /// <summary>
    /// Reserva con sus mesas. Por defecto: Confirmada, de hoy, hora reciente y con la llegada registrada (sentada).
    /// <paramref name="atendida"/> = false la deja "esperando llegada".
    /// </summary>
    public Reserva CrearReserva(
        Mesa[]? mesas = null,
        string estado = "Confirmada",
        DateOnly? fecha = null,
        TimeOnly? hora = null,
        bool atendida = true,
        int personas = 2,
        bool conMesa = true,
        DateTime? fechaAtencion = null)
    {
        using var db = NuevoContexto();
        var n = Siguiente();
        var cliente = new Cliente
        {
            Nombre = $"Cliente{n}", Apellido = "Prueba", Telefono = $"8{n % 10_000_000:D7}", FechaCreacion = DateTime.Now
        };
        db.Clientes.Add(cliente);
        db.SaveChanges();

        var reserva = new Reserva
        {
            CodigoReserva = $"T{n % 1_000_000_000:D9}",
            IdCliente = cliente.IdCliente,
            IdEstadoReserva = db.EstadosReserva.Single(e => e.Nombre == estado).IdEstadoReserva,
            FechaReserva = fecha ?? Hoy,
            HoraReserva = hora ?? HoraReciente(),
            CantidadPersonas = personas,
            FechaCreacion = DateTime.Now
        };
        if (atendida) reserva.FechaAtencion = fechaAtencion ?? DateTime.Now.AddMinutes(-5);

        if (conMesa)
        {
            foreach (var mesa in mesas is { Length: > 0 } ? mesas : [CrearMesa()])
                reserva.ReservaMesas.Add(new ReservaMesa { IdMesa = mesa.IdMesa });
        }

        db.Reservas.Add(reserva);
        db.SaveChanges();
        return reserva;
    }

    public (Reserva Reserva, Mesa Mesa) CrearReservaSentada(string estado = "Confirmada")
    {
        var mesa = CrearMesa();
        return (CrearReserva([mesa], estado), mesa);
    }

    // ---------- Lecturas ----------

    public Venta? VentaDe(int idReserva)
    {
        using var db = NuevoContexto();
        return db.Ventas.AsNoTracking()
            .Include(v => v.Detalles).Include(v => v.Pedidos)
            .SingleOrDefault(v => v.IdReserva == idReserva);
    }

    public int CantidadVentas(int idReserva)
    {
        using var db = NuevoContexto();
        return db.Ventas.Count(v => v.IdReserva == idReserva);
    }

    public Reserva ReservaActual(int idReserva)
    {
        using var db = NuevoContexto();
        return db.Reservas.AsNoTracking().Include(r => r.EstadoReserva).Single(r => r.IdReserva == idReserva);
    }

    public void Dispose() => _sesion.Dispose();
}
