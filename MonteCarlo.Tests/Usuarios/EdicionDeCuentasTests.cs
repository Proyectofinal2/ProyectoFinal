using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;
using MonteCarlo.Tests.Infra;

namespace MonteCarlo.Tests.Usuarios;

/// <summary>
/// HU-AUT-007: editar cuentas de administrador y auditar quién hizo cada cambio (alta, edición, habilitar y deshabilitar).
/// Las reglas del último General activo y del propio rol protegen el acceso al sistema.
/// </summary>
public class EdicionDeCuentasTests(SqlServerFixture fixture) : PruebaSql(fixture)
{
    private static UpdateUserRequest Cambios(Usuario u, Action<UpdateUserRequest>? ajustar = null)
    {
        var request = new UpdateUserRequest
        {
            NombreCompleto = u.NombreCompleto, NombreUsuario = u.NombreUsuario,
            CorreoElectronico = u.CorreoElectronico, CorreoPersonal = u.CorreoPersonal, Rol = u.Rol
        };
        ajustar?.Invoke(request);
        return request;
    }

    private async Task<List<HistorialUsuario>> HistorialDeAsync(int idUsuarioAfectado)
    {
        await using var db = Mundo.NuevoContexto();
        return await db.HistorialUsuarios.AsNoTracking()
            .Where(h => h.IdUsuarioAfectado == idUsuarioAfectado).OrderBy(h => h.IdHistorialUsuario).ToListAsync();
    }

    private async Task<Usuario> GuardadoAsync(int id)
    {
        await using var db = Mundo.NuevoContexto();
        return await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuario == id);
    }

    /// <summary>La base de pruebas es compartida: deja una sola cuenta General activa mientras corre la acción.</summary>
    private async Task ConUnicoGeneralAsync(int idGeneral, Func<Task> accion)
    {
        await using var db = Mundo.NuevoContexto();
        var otros = await db.Usuarios.Where(u => u.Rol == "General" && u.Activo && u.IdUsuario != idGeneral)
            .Select(u => u.IdUsuario).ToListAsync();
        await db.Usuarios.Where(u => otros.Contains(u.IdUsuario)).ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
        try
        {
            await accion();
        }
        finally
        {
            await db.Usuarios.Where(u => otros.Contains(u.IdUsuario)).ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, true));
        }
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "1")]
    public async Task Actualizar_variosCampos_registraUnaFilaPorCampoConElResponsable()
    {
        var cuenta = Mundo.CrearUsuario();
        var responsable = Mundo.CrearUsuario("General");

        var r = await S.Usuarios.ActualizarAsync(cuenta.IdUsuario, Cambios(cuenta, c =>
        {
            c.NombreCompleto = "Nombre Nuevo";
            c.Rol = "General";
            c.CorreoPersonal = "personal@prueba.test";
        }), responsable.IdUsuario);

        Assert.True(r.Success, r.Message);
        var guardada = await GuardadoAsync(cuenta.IdUsuario);
        Assert.Equal("Nombre Nuevo", guardada.NombreCompleto);
        Assert.Equal("General", guardada.Rol);

        var historial = await HistorialDeAsync(cuenta.IdUsuario);
        Assert.Equal(3, historial.Count);
        Assert.All(historial, h =>
        {
            Assert.Equal(HistorialUsuarioAcciones.Editada, h.Accion);
            Assert.Equal(responsable.IdUsuario, h.IdUsuario);
        });
        var rol = historial.Single(h => h.Campo == HistorialUsuarioCampos.Rol);
        Assert.Equal("Regular", rol.ValorAnterior);
        Assert.Equal("General", rol.ValorNuevo);
        var correo = historial.Single(h => h.Campo == HistorialUsuarioCampos.CorreoPersonal);
        Assert.Null(correo.ValorAnterior);
        Assert.Equal("personal@prueba.test", correo.ValorNuevo);
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "2")]
    public async Task Actualizar_sinCambios_noRegistraHistorial()
    {
        var cuenta = Mundo.CrearUsuario();
        var responsable = Mundo.CrearUsuario("General");

        var r = await S.Usuarios.ActualizarAsync(cuenta.IdUsuario, Cambios(cuenta), responsable.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Empty(await HistorialDeAsync(cuenta.IdUsuario));
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "3")]
    public async Task Actualizar_correoDeOtraCuenta_devuelve409YNoGuardaNadaNiHistorial()
    {
        var cuenta = Mundo.CrearUsuario();
        var otra = Mundo.CrearUsuario();
        var responsable = Mundo.CrearUsuario("General");

        var r = await S.Usuarios.ActualizarAsync(cuenta.IdUsuario, Cambios(cuenta, c =>
        {
            c.NombreCompleto = "No debe guardarse";
            c.CorreoElectronico = otra.CorreoElectronico;
        }), responsable.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        Assert.Contains("correo", r.Message);
        var guardada = await GuardadoAsync(cuenta.IdUsuario);
        Assert.Equal(cuenta.NombreCompleto, guardada.NombreCompleto);
        Assert.Equal(cuenta.CorreoElectronico, guardada.CorreoElectronico);
        Assert.Empty(await HistorialDeAsync(cuenta.IdUsuario));
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "4")]
    public async Task Actualizar_elPropioRol_devuelve400YNoCambiaNada()
    {
        var yo = Mundo.CrearUsuario("General");

        var r = await S.Usuarios.ActualizarAsync(yo.IdUsuario, Cambios(yo, c => c.Rol = "Regular"), yo.IdUsuario);

        Assert.Equal(400, r.StatusCode);
        Assert.Equal("General", (await GuardadoAsync(yo.IdUsuario)).Rol);
        Assert.Empty(await HistorialDeAsync(yo.IdUsuario));
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "5")]
    public async Task Actualizar_quitarElRolALaUltimaGeneralActiva_devuelve409()
    {
        var ultima = Mundo.CrearUsuario("General");
        var responsable = Mundo.CrearUsuario("Regular");

        await ConUnicoGeneralAsync(ultima.IdUsuario, async () =>
        {
            var r = await S.Usuarios.ActualizarAsync(ultima.IdUsuario, Cambios(ultima, c => c.Rol = "Regular"), responsable.IdUsuario);

            Assert.Equal(409, r.StatusCode);
        });

        Assert.Equal("General", (await GuardadoAsync(ultima.IdUsuario)).Rol);
        Assert.Empty(await HistorialDeAsync(ultima.IdUsuario));
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "5")]
    public async Task Desactivar_laUltimaGeneralActiva_devuelve409()
    {
        var ultima = Mundo.CrearUsuario("General");
        var responsable = Mundo.CrearUsuario("Regular");

        await ConUnicoGeneralAsync(ultima.IdUsuario, async () =>
        {
            var r = await S.Usuarios.CambiarEstadoAsync(ultima.IdUsuario, activo: false, responsable.IdUsuario);

            Assert.Equal(409, r.StatusCode);
        });

        Assert.True((await GuardadoAsync(ultima.IdUsuario)).Activo);
        Assert.Empty(await HistorialDeAsync(ultima.IdUsuario));
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "6")]
    public async Task CambiarEstado_desactivarYReactivar_registraResponsableYMotivo()
    {
        var cuenta = Mundo.CrearUsuario();
        var responsable = Mundo.CrearUsuario("General");

        var baja = await S.Usuarios.CambiarEstadoAsync(cuenta.IdUsuario, activo: false, responsable.IdUsuario, "  Se fue de la empresa ");
        var alta = await S.Usuarios.CambiarEstadoAsync(cuenta.IdUsuario, activo: true, responsable.IdUsuario);

        Assert.True(baja.Success && alta.Success);
        Assert.True((await GuardadoAsync(cuenta.IdUsuario)).Activo);
        var historial = await HistorialDeAsync(cuenta.IdUsuario);
        Assert.Collection(historial,
            h =>
            {
                Assert.Equal(HistorialUsuarioAcciones.Deshabilitada, h.Accion);
                Assert.Equal(responsable.IdUsuario, h.IdUsuario);
                Assert.Equal("Se fue de la empresa", h.Motivo);
                Assert.Equal("Sí", h.ValorAnterior);
                Assert.Equal("No", h.ValorNuevo);
            },
            h =>
            {
                Assert.Equal(HistorialUsuarioAcciones.Habilitada, h.Accion);
                Assert.Equal(responsable.IdUsuario, h.IdUsuario);
                Assert.Null(h.Motivo);
            });
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "6")]
    public async Task CambiarEstado_cuentaQueYaEstabaEnEseEstado_noRegistraHistorial()
    {
        var inactiva = Mundo.CrearUsuario(activo: false);
        var responsable = Mundo.CrearUsuario("General");

        var r = await S.Usuarios.CambiarEstadoAsync(inactiva.IdUsuario, activo: false, responsable.IdUsuario);

        Assert.True(r.Success, r.Message);
        Assert.Empty(await HistorialDeAsync(inactiva.IdUsuario));
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "7")]
    public async Task Crear_registraLaCuentaYQuienLaCreoEnElMismoGuardado()
    {
        var responsable = Mundo.CrearUsuario("General");
        var n = Guid.NewGuid().ToString("N")[..8];

        var r = await S.Usuarios.CreateUserAsync(new CreateUserRequest
        {
            NombreCompleto = $"Alta {n}", NombreUsuario = $"alta.{n}", CorreoElectronico = $"alta{n}@prueba.test",
            ContrasenaInicial = "Prueba123", Rol = "Regular"
        }, responsable.IdUsuario);

        Assert.True(r.Success, r.Message);
        var historial = await HistorialDeAsync(r.Data!.IdUsuario);
        var fila = Assert.Single(historial);
        Assert.Equal(HistorialUsuarioAcciones.Creada, fila.Accion);
        Assert.Equal(responsable.IdUsuario, fila.IdUsuario);
    }

    [SqlFact]
    [Trait("HU", "AUT-007"), Trait("Escenario", "7")]
    public async Task Crear_conUsuarioDuplicado_devuelve409YNoDejaHistorial()
    {
        var existente = Mundo.CrearUsuario();
        var responsable = Mundo.CrearUsuario("General");
        var n = Guid.NewGuid().ToString("N")[..8];

        var r = await S.Usuarios.CreateUserAsync(new CreateUserRequest
        {
            NombreCompleto = $"Alta {n}", NombreUsuario = existente.NombreUsuario, CorreoElectronico = $"alta{n}@prueba.test",
            ContrasenaInicial = "Prueba123", Rol = "Regular"
        }, responsable.IdUsuario);

        Assert.Equal(409, r.StatusCode);
        await using var db = Mundo.NuevoContexto();
        Assert.False(await db.HistorialUsuarios.AnyAsync(h => h.NombreUsuarioAfectado == $"Alta {n}"));
    }
}
