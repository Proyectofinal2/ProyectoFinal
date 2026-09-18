using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data;

/// <summary>
/// Contexto de Entity Framework Core del sistema de reservas Mirador Monte Carlo.
/// El mapeo refleja el esquema definido en Data/database.sql.
/// </summary>
public class MonteCarloDbContext : DbContext
{
    public MonteCarloDbContext(DbContextOptions<MonteCarloDbContext> options)
        : base(options)
    {
    }

    public DbSet<EstadoReserva> EstadosReserva => Set<EstadoReserva>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<HorarioOperacion> HorariosOperacion => Set<HorarioOperacion>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<CierreFijo> CierresFijos => Set<CierreFijo>();
    public DbSet<CierreEventual> CierresEventuales => Set<CierreEventual>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<HistorialCierre> HistorialCierres => Set<HistorialCierre>();
    public DbSet<ConfiguracionSistema> ConfiguracionesSistema => Set<ConfiguracionSistema>();
    public DbSet<TokenRecuperacionContrasena> TokensRecuperacionContrasena => Set<TokenRecuperacionContrasena>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Todas las tablas viven en el esquema dbo y conservan el nombre singular del script.
        modelBuilder.HasDefaultSchema("dbo");

        ConfigurarEstadoReserva(modelBuilder);
        ConfigurarCliente(modelBuilder);
        ConfigurarMesa(modelBuilder);
        ConfigurarUsuario(modelBuilder);
        ConfigurarHorarioOperacion(modelBuilder);
        ConfigurarMetodoPago(modelBuilder);
        ConfigurarProducto(modelBuilder);
        ConfigurarCierreFijo(modelBuilder);
        ConfigurarCierreEventual(modelBuilder);
        ConfigurarReserva(modelBuilder);
        ConfigurarNotificacion(modelBuilder);
        ConfigurarHistorialCierre(modelBuilder);
        ConfigurarConfiguracionSistema(modelBuilder);
        ConfigurarTokenRecuperacionContrasena(modelBuilder);
        ConfigurarVenta(modelBuilder);
        ConfigurarDetalleVenta(modelBuilder);
    }

    private static void ConfigurarEstadoReserva(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EstadoReserva>(entity =>
        {
            entity.ToTable("EstadoReserva");
            entity.HasKey(e => e.IdEstadoReserva).HasName("PK_EstadoReserva");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_EstadoReserva_Nombre");

            entity.Property(e => e.Nombre).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(200);
        });
    }

    private static void ConfigurarCliente(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Cliente");
            entity.HasKey(e => e.IdCliente).HasName("PK_Cliente");

            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Apellido).HasMaxLength(80);
            entity.Property(e => e.Telefono).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CorreoElectronico).HasMaxLength(120);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
        });
    }

    private static void ConfigurarMesa(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.ToTable("Mesa", t => t.HasCheckConstraint("CK_Mesa_Capacidad", "Capacidad > 0"));
            entity.HasKey(e => e.IdMesa).HasName("PK_Mesa");
            entity.HasIndex(e => e.NumeroMesa).IsUnique().HasDatabaseName("UQ_Mesa_Numero");

            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Disponible");
        });
    }

    private static void ConfigurarUsuario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario", t => t.HasCheckConstraint(
                "CK_Usuario_Rol", "Rol IN ('General', 'Regular')"));
            entity.HasKey(e => e.IdUsuario).HasName("PK_Usuario");
            entity.HasIndex(e => e.NombreUsuario).IsUnique().HasDatabaseName("UQ_Usuario_NombreUsuario");
            entity.HasIndex(e => e.CorreoElectronico).IsUnique().HasDatabaseName("UQ_Usuario_Correo");

            entity.Property(e => e.NombreUsuario).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NombreCompleto).HasMaxLength(120).IsRequired();
            entity.Property(e => e.CorreoElectronico).HasMaxLength(120).IsRequired();
            entity.Property(e => e.CorreoPersonal).HasMaxLength(120).IsRequired(false);
            entity.Property(e => e.Rol).HasMaxLength(20).IsRequired().HasDefaultValue("Regular");
            entity.Property(e => e.ContrasenaHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
        });
    }

    private static void ConfigurarHorarioOperacion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HorarioOperacion>(entity =>
        {
            entity.ToTable("HorarioOperacion", t =>
            {
                t.HasCheckConstraint("CK_HorarioOperacion_Rango", "HoraCierre > HoraApertura");
                t.HasCheckConstraint("CK_HorarioOperacion_Dia", "DiaSemana BETWEEN 0 AND 6");
            });
            entity.HasKey(e => e.IdHorarioOperacion).HasName("PK_HorarioOperacion");

            entity.Property(e => e.Activo).HasDefaultValue(true);
        });
    }

    private static void ConfigurarMetodoPago(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.ToTable("MetodoPago");
            entity.HasKey(e => e.IdMetodoPago).HasName("PK_MetodoPago");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_MetodoPago_Nombre");

            entity.Property(e => e.Nombre).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });
    }

    private static void ConfigurarProducto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Producto", t => t.HasCheckConstraint("CK_Producto_Precio", "Precio >= 0"));
            entity.HasKey(e => e.IdProducto).HasName("PK_Producto");

            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Categoria).HasMaxLength(50);
            entity.Property(e => e.Precio).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Disponible).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
        });
    }

    private static void ConfigurarCierreFijo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CierreFijo>(entity =>
        {
            entity.ToTable("CierreFijo", t => t.HasCheckConstraint(
                "CK_CierreFijo_Dia", "DiaSemana BETWEEN 0 AND 6"));
            entity.HasKey(e => e.IdCierreFijo).HasName("PK_CierreFijo");
            entity.HasIndex(e => e.DiaSemana).IsUnique().HasDatabaseName("UQ_CierreFijo_Dia");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.UsuarioCreacion)
                .WithMany(u => u.CierresFijos)
                .HasForeignKey(e => e.IdUsuarioCreacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CierreFijo_Usuario");
        });
    }

    private static void ConfigurarCierreEventual(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CierreEventual>(entity =>
        {
            entity.ToTable("CierreEventual");
            entity.HasKey(e => e.IdCierreEventual).HasName("PK_CierreEventual");
            entity.HasIndex(e => e.Fecha).IsUnique().HasDatabaseName("UQ_CierreEventual_Fecha");

            entity.Property(e => e.Motivo).HasMaxLength(300).IsRequired();
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.UsuarioCreacion)
                .WithMany(u => u.CierresEventuales)
                .HasForeignKey(e => e.IdUsuarioCreacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CierreEventual_Usuario");
        });
    }

    private static void ConfigurarReserva(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reserva", t => t.HasCheckConstraint(
                "CK_Reserva_CantidadPersonas", "CantidadPersonas > 0"));
            entity.HasKey(e => e.IdReserva).HasName("PK_Reserva");
            entity.HasIndex(e => e.CodigoReserva).IsUnique().HasDatabaseName("UQ_Reserva_Codigo");
            entity.HasIndex(e => new { e.FechaReserva, e.HoraReserva }).HasDatabaseName("IX_Reserva_FechaHora");
            entity.HasIndex(e => e.IdMesa).HasDatabaseName("IX_Reserva_Mesa");

            entity.Property(e => e.CodigoReserva).HasMaxLength(10).IsRequired();
            entity.Property(e => e.RequiereAsignacionManual).HasDefaultValue(false);
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(300);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.Reservas)
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reserva_Cliente");

            entity.HasOne(e => e.Mesa)
                .WithMany(m => m.Reservas)
                .HasForeignKey(e => e.IdMesa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reserva_Mesa");

            entity.HasOne(e => e.EstadoReserva)
                .WithMany(s => s.Reservas)
                .HasForeignKey(e => e.IdEstadoReserva)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reserva_EstadoReserva");

            entity.HasOne(e => e.CierreEventual)
                .WithMany(c => c.ReservasCanceladas)
                .HasForeignKey(e => e.IdCierreEventual)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reserva_CierreEventual");
        });
    }

    private static void ConfigurarNotificacion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.ToTable("Notificacion");
            entity.HasKey(e => e.IdNotificacion).HasName("PK_Notificacion");
            entity.HasIndex(e => e.IdReserva).HasDatabaseName("IX_Notificacion_Reserva");

            entity.Property(e => e.TipoNotificacion).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Canal).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Destinatario).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.EstadoEnvio).HasMaxLength(20).IsRequired().HasDefaultValue("Pendiente");
            entity.Property(e => e.DetalleError).HasMaxLength(300);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Reserva)
                .WithMany(r => r.Notificaciones)
                .HasForeignKey(e => e.IdReserva)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Notificacion_Reserva");
        });
    }

    private static void ConfigurarHistorialCierre(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistorialCierre>(entity =>
        {
            entity.ToTable("HistorialCierre", t =>
            {
                t.HasCheckConstraint("CK_HistorialCierre_Tipo", "TipoCierre IN ('Fijo', 'Eventual')");
                t.HasCheckConstraint("CK_HistorialCierre_Accion", "Accion IN ('Registrado', 'Eliminado')");
            });
            entity.HasKey(e => e.IdHistorialCierre).HasName("PK_HistorialCierre");
            entity.HasIndex(e => e.Fecha).HasDatabaseName("IX_HistorialCierre_Fecha");
            entity.HasIndex(e => e.TipoCierre).HasDatabaseName("IX_HistorialCierre_Tipo");
            entity.HasIndex(e => e.IdUsuario).HasDatabaseName("IX_HistorialCierre_Usuario");

            entity.Property(e => e.TipoCierre).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Accion).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.Fecha).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.HistorialCierres)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HistorialCierre_Usuario");

            entity.HasOne(e => e.CierreEventual)
                .WithMany(c => c.HistorialCierres)
                .HasForeignKey(e => e.IdCierreEventual)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HistorialCierre_CierreEventual");
        });
    }

    private static void ConfigurarConfiguracionSistema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfiguracionSistema>(entity =>
        {
            entity.ToTable("ConfiguracionSistema");
            entity.HasKey(e => e.Clave).HasName("PK_ConfiguracionSistema");

            entity.Property(e => e.Clave).HasMaxLength(50);
            entity.Property(e => e.Valor).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.UsuarioModificacion)
                .WithMany(u => u.ConfiguracionesModificadas)
                .HasForeignKey(e => e.IdUsuarioModificacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConfiguracionSistema_Usuario");
        });
    }

    private static void ConfigurarTokenRecuperacionContrasena(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TokenRecuperacionContrasena>(entity =>
        {
            entity.ToTable("TokenRecuperacionContrasena");
            entity.HasKey(e => e.IdToken).HasName("PK_TokenRecuperacionContrasena");
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("UQ_TokenRecuperacion_Hash");

            entity.Property(e => e.TokenHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.Utilizado).HasDefaultValue(false);

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.TokensRecuperacion)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TokenRecuperacion_Usuario");
        });
    }

    private static void ConfigurarVenta(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("Venta", t =>
            {
                t.HasCheckConstraint("CK_Venta_Tipo", "TipoVenta IN ('Consumo', 'Takeout')");
                t.HasCheckConstraint("CK_Venta_Total", "Total >= 0");
                t.HasCheckConstraint(
                    "CK_Venta_TipoReserva",
                    "(TipoVenta = 'Consumo' AND IdReserva IS NOT NULL) OR (TipoVenta = 'Takeout' AND IdReserva IS NULL)");
            });
            entity.HasKey(e => e.IdVenta).HasName("PK_Venta");
            entity.HasIndex(e => e.FechaVenta).HasDatabaseName("IX_Venta_Fecha");
            entity.HasIndex(e => e.TipoVenta).HasDatabaseName("IX_Venta_Tipo");

            entity.Property(e => e.TipoVenta).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Total).HasColumnType("decimal(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.FechaVenta).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Reserva)
                .WithMany(r => r.Ventas)
                .HasForeignKey(e => e.IdReserva)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Venta_Reserva");

            entity.HasOne(e => e.MetodoPago)
                .WithMany(m => m.Ventas)
                .HasForeignKey(e => e.IdMetodoPago)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Venta_MetodoPago");

            entity.HasOne(e => e.UsuarioRegistro)
                .WithMany(u => u.VentasRegistradas)
                .HasForeignKey(e => e.IdUsuarioRegistro)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Venta_Usuario");
        });
    }

    private static void ConfigurarDetalleVenta(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.ToTable("DetalleVenta", t =>
            {
                t.HasCheckConstraint("CK_DetalleVenta_Cantidad", "Cantidad > 0");
                t.HasCheckConstraint("CK_DetalleVenta_Precio", "PrecioUnitario >= 0");
            });
            entity.HasKey(e => e.IdDetalleVenta).HasName("PK_DetalleVenta");
            entity.HasIndex(e => e.IdVenta).HasDatabaseName("IX_DetalleVenta_Venta");
            entity.HasIndex(e => e.IdProducto).HasDatabaseName("IX_DetalleVenta_Producto");

            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10,2)");

            // Columna calculada PERSISTED: la base de datos la genera, EF solo la lee.
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(21,2)")
                .HasComputedColumnSql("(Cantidad * PrecioUnitario)", stored: true);

            entity.HasOne(e => e.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(e => e.IdVenta)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DetalleVenta_Venta");

            entity.HasOne(e => e.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DetalleVenta_Producto");
        });
    }
}
