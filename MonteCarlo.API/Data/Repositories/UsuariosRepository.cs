using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

/// <summary>
/// Repositorio para la gestión de cuentas de administrador.
/// HU-AUT-006: consultar, desactivar y reactivar cuentas. HU-AUT-007: editar cuentas con historial.
/// </summary>
public class UsuariosRepository(MonteCarloDbContext context) : IUsuariosRepository
{
    private const string MensajeUsuarioDuplicado = "Ya existe una cuenta con este nombre de usuario.";
    private const string MensajeCorreoDuplicado = "Ya existe una cuenta con este correo corporativo.";

    /// <summary>
    /// Crea un nuevo usuario en la base de datos junto con su fila de historial.
    /// Valida que el nombre de usuario y correo corporativo sean únicos.
    /// HU-AUT-005: crear cuenta de administrador.
    /// </summary>
    public async Task<(bool Success, string Message, Usuario? CreatedUser)>
        CrearUsuarioAsync(Usuario usuario, HistorialUsuario historial)
    {
        // Validar que el nombre de usuario sea único
        var usuarioExistente = await context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == usuario.NombreUsuario);

        if (usuarioExistente != null)
        {
            return (false, MensajeUsuarioDuplicado, null);
        }

        // Validar que el correo corporativo sea único
        var correoExistente = await context.Usuarios
            .FirstOrDefaultAsync(u => u.CorreoElectronico == usuario.CorreoElectronico);

        if (correoExistente != null)
        {
            return (false, MensajeCorreoDuplicado, null);
        }

        usuario.FechaCreacion = DateTime.UtcNow;
        context.Usuarios.Add(usuario);
        var (success, message) = await GuardarCambiosAsync([historial]);

        return success
            ? (true, "Cuenta creada exitosamente.", usuario)
            : (false, message, null);
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

    public Task<int> ContarOtrosGeneralesActivosAsync(int idExcluido) =>
        context.Usuarios.CountAsync(u => u.Rol == "General" && u.Activo && u.IdUsuario != idExcluido);

    /// <summary>
    /// Guarda los cambios pendientes de las cuentas rastreadas junto con sus filas de historial.
    /// Los índices únicos (UQ_Usuario_NombreUsuario / UQ_Usuario_Correo) son la última línea de defensa
    /// ante una carrera entre la validación y el guardado; no se expone el mensaje crudo de SQL Server.
    /// </summary>
    public async Task<(bool Success, string Message)> GuardarCambiosAsync(IEnumerable<HistorialUsuario> historial)
    {
        try
        {
            context.HistorialUsuarios.AddRange(historial);
            await context.SaveChangesAsync();
            return (true, string.Empty);
        }
        catch (DbUpdateException ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;

            if (detalle.Contains("UQ_Usuario_NombreUsuario"))
            {
                return (false, MensajeUsuarioDuplicado);
            }

            if (detalle.Contains("UQ_Usuario_Correo"))
            {
                return (false, MensajeCorreoDuplicado);
            }

            return (false, "No se pudo guardar la cuenta. Intenta nuevamente.");
        }
    }
}
