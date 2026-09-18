using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repository for administrator account management operations.
    /// </summary>
    public interface IUsuariosRepository
    {
        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="usuario">The user to create.</param>
        /// <returns>
        /// A tuple indicating success, an informational message, and the created user (or null on failure).
        /// </returns>
        Task<(bool Success, string Message, Usuario? CreatedUser)> CrearUsuarioAsync(Usuario usuario);

        /// <summary>
        /// Gets all users, optionally filtered by search text and active status.
        /// </summary>
        /// <param name="busqueda">Text to match against name, username or email; null/empty for no filter.</param>
        /// <param name="soloActivos">True for active only, false for inactive only, null for all.</param>
        Task<List<Usuario>> ObtenerTodosAsync(string? busqueda, bool? soloActivos);

        /// <summary>
        /// Gets a user by id.
        /// </summary>
        Task<Usuario?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Changes a user's active status.
        /// </summary>
        /// <returns>A tuple indicating success and an informational message.</returns>
        Task<(bool Success, string Message)> CambiarEstadoAsync(int id, bool activo);
    }
}
