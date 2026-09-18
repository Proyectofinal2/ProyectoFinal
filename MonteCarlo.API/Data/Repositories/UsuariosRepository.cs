using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

/// <summary>
/// Repositorio para la gestión de cuentas de administrador.
/// HU-AUT-006: consultar, desactivar y reactivar cuentas.
/// </summary>
public class UsuariosRepository(MonteCarloDbContext context) : IUsuariosRepository
{
    /// <summary>
    /// Crea un nuevo usuario en la base de datos.
    /// Valida que el nombre de usuario y correo corporativo sean únicos.
    /// HU-AUT-005: crear cuenta de administrador.
    /// </summary>
    public async Task<(bool Success, string Message, Usuario? CreatedUser)>
        CrearUsuarioAsync(Usuario usuario)
    {
        // Validar que el nombre de usuario sea único
        var usuarioExistente = await context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == usuario.NombreUsuario);

        if (usuarioExistente != null)
        {
            return (false, "Ya existe una cuenta con este nombre de usuario.", null);
        }

        // Validar que el correo corporativo sea único
        var correoExistente = await context.Usuarios
            .FirstOrDefaultAsync(u => u.CorreoElectronico == usuario.CorreoElectronico);

        if (correoExistente != null)
        {
            return (false, "Ya existe una cuenta con este correo corporativo.", null);
        }

        try
        {
            usuario.FechaCreacion = DateTime.UtcNow;
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
            return (true, "Cuenta creada exitosamente.", usuario);
        }
        catch (DbUpdateException ex)
        {
            // Carrera entre la validación previa y el INSERT: otra solicitud pudo
            // haber creado un usuario con el mismo dato entre el SELECT y este punto.
            // Los índices únicos de la tabla (UQ_Usuario_NombreUsuario / UQ_Usuario_Correo)
            // son la última línea de defensa; no exponemos el mensaje crudo de SQL Server.
            var detalle = ex.InnerException?.Message ?? ex.Message;

            if (detalle.Contains("UQ_Usuario_NombreUsuario"))
            {
                return (false, "Ya existe una cuenta con este nombre de usuario.", null);
            }

            if (detalle.Contains("UQ_Usuario_Correo"))
            {
                return (false, "Ya existe una cuenta con este correo corporativo.", null);
            }

            return (false, "No se pudo crear la cuenta. Intenta nuevamente.", null);
        }
    }

    /// <summary>
    /// Obtiene todos los usuarios, filtrando opcionalmente por texto de búsqueda y estado activo.
    /// </summary>
    public async Task<List<Usuario>> ObtenerTodosAsync(string? busqueda, bool? soloActivos)
    {
        var query = context.Usuarios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(u =>
                EF.Functions.Like(u.NombreCompleto, $"%{busqueda}%") ||
                EF.Functions.Like(u.NombreUsuario, $"%{busqueda}%") ||
                EF.Functions.Like(u.CorreoElectronico, $"%{busqueda}%"));
        }

        if (soloActivos.HasValue)
        {
            query = query.Where(u => u.Activo == soloActivos.Value);
        }

        return await query.OrderBy(u => u.NombreCompleto).ToListAsync();
    }

    /// <summary>
    /// Obtiene un usuario por su id.
    /// </summary>
    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        return await context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id);
    }

    /// <summary>
    /// Cambia el estado activo/inactivo de un usuario.
    /// HU-AUT-006, escenarios 2 y 3: desactivar y reactivar cuentas.
    /// </summary>
    public async Task<(bool Success, string Message)> CambiarEstadoAsync(int id, bool activo)
    {
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id);

        if (usuario == null)
        {
            return (false, "No se encontró la cuenta.");
        }

        usuario.Activo = activo;
        await context.SaveChangesAsync();

        return (true, activo ? "La cuenta fue reactivada." : "La cuenta fue desactivada.");
    }
}
