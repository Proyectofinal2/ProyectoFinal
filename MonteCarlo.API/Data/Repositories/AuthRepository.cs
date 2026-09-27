using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

/// <summary>
/// Repositorio para operaciones de autenticación.
/// Maneja búsquedas de usuarios, validación de bloqueos, etc.
/// </summary>
public class AuthRepository(MonteCarloDbContext context) : IAuthRepository
{

    /// <summary>
    /// Obtiene un usuario por nombre de usuario o correo electrónico.
    /// </summary>
    public async Task<Usuario?> ObtenerPorNombreOCorreoAsync(string usuarioOCorreo)
    {
        return await context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.NombreUsuario == usuarioOCorreo ||
                u.CorreoElectronico == usuarioOCorreo);
    }

    /// <summary>
    /// Registra un intento fallido de login en el usuario.
    /// Si se alcanzan 3 intentos, bloquea temporalmente la cuenta.
    /// HU-AUT-001, escenario 3: bloqueo por 5 minutos.
    /// </summary>
    public async Task RegistrarIntentoFallidoAsync(Usuario usuario)
    {
        usuario.IntentosFallidos++;

        if (usuario.IntentosFallidos >= 3)
        {
            usuario.FechaBloqueoHasta = DateTime.UtcNow.AddMinutes(5);
        }

        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Resetea los intentos fallidos de un usuario después de login exitoso.
    /// </summary>
    public async Task RestablecerIntentosFallidosAsync(Usuario usuario)
    {
        usuario.IntentosFallidos = 0;
        usuario.FechaBloqueoHasta = null;

        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Verifica si una cuenta está temporalmente bloqueada.
    /// HU-AUT-001, escenario 3.
    /// </summary>
    public bool EstaBloqueadoTemporalmente(Usuario usuario)
    {
        return usuario.FechaBloqueoHasta.HasValue &&
               usuario.FechaBloqueoHasta > DateTime.UtcNow;
    }

    /// <summary>
    /// Invalida los tokens de recuperación activos de un usuario.
    /// HU-AUT-003: garantiza un único token activo por usuario.
    /// </summary>
    public async Task InvalidarTokensActivosAsync(int idUsuario)
    {
        var tokensActivos = await context.TokensRecuperacionContrasena
            .Where(t => t.IdUsuario == idUsuario &&
                        !t.Utilizado &&
                        t.FechaExpiracion > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in tokensActivos)
        {
            token.Utilizado = true;
            token.FechaUtilizado = DateTime.UtcNow;
        }

        if (tokensActivos.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Crea un nuevo token de recuperación de contraseña. HU-AUT-003.
    /// </summary>
    public async Task<TokenRecuperacionContrasena> CrearTokenRecuperacionAsync(int idUsuario, string tokenHash, DateTime fechaExpiracion)
    {
        var token = new TokenRecuperacionContrasena
        {
            IdUsuario = idUsuario,
            TokenHash = tokenHash,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = fechaExpiracion,
            Utilizado = false
        };

        context.TokensRecuperacionContrasena.Add(token);
        await context.SaveChangesAsync();

        return token;
    }

    /// <summary>
    /// Obtiene un token de recuperación válido (no usado y no expirado) por su hash. HU-AUT-003.
    /// </summary>
    public async Task<TokenRecuperacionContrasena?> ObtenerTokenValidoAsync(string tokenHash)
    {
        return await context.TokensRecuperacionContrasena
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                !t.Utilizado &&
                t.FechaExpiracion > DateTime.UtcNow);
    }

    /// <summary>
    /// Actualiza la contraseña de un usuario, limpia su estado de bloqueo y marca el
    /// token de recuperación como utilizado, en una única transacción. HU-AUT-003.
    /// </summary>
    public async Task RestablecerContrasenaAsync(Usuario usuario, TokenRecuperacionContrasena token, string nuevoHash)
    {
        usuario.ContrasenaHash = nuevoHash;
        usuario.IntentosFallidos = 0;
        usuario.FechaBloqueoHasta = null;

        token.Utilizado = true;
        token.FechaUtilizado = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }
}
