using MonteCarlo.API.Data.Entities;
using System.Threading.Tasks;

namespace MonteCarlo.API.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repository for authentication and user-related operations.
    /// </summary>
    public interface IAuthRepository
    {
        /// <summary>
        /// Gets a user by username or email.
        /// </summary>
        /// <param name="usuarioOCorreo">The username or email to search for.</param>
        /// <returns>The matching <see cref="Usuario"/> if found; otherwise null.</returns>
        Task<Usuario?> ObtenerPorNombreOCorreoAsync(string usuarioOCorreo);

        /// <summary>
        /// Registers a failed login attempt for the specified user.
        /// </summary>
        /// <param name="usuario">The user to update.</param>
        Task RegistrarIntentoFallidoAsync(Usuario usuario);

        /// <summary>
        /// Resets the failed login attempts for the specified user.
        /// </summary>
        /// <param name="usuario">The user to update.</param>
        Task RestablecerIntentosFallidosAsync(Usuario usuario);

        /// <summary>
        /// Determines whether the user is temporarily blocked.
        /// </summary>
        /// <param name="usuario">The user to check.</param>
        /// <returns>True if the user is temporarily blocked; otherwise false.</returns>
        bool EstaBloqueadoTemporalmente(Usuario usuario);
    }
}
