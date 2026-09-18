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
}
