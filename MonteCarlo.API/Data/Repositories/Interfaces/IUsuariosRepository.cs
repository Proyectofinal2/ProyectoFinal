using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repository for administrator account management operations.
    /// </summary>
    public interface IUsuariosRepository
    {
        /// <summary>
        /// Creates a new user and its audit row in a single SaveChanges (HU-AUT-007).
        /// </summary>
        /// <param name="usuario">The user to create.</param>
        /// <param name="historial">Audit row (navigation to <paramref name="usuario"/>) saved with the account.</param>
        /// <returns>
        /// A tuple indicating success, an informational message, and the created user (or null on failure).
        /// </returns>
        Task<(bool Success, string Message, Usuario? CreatedUser)> CrearUsuarioAsync(Usuario usuario, HistorialUsuario historial);

        /// <summary>
        /// Gets all users, optionally filtered by search text and active status.
        /// </summary>
        /// <param name="busqueda">Text to match against name, username or email; null/empty for no filter.</param>
        /// <param name="soloActivos">True for active only, false for inactive only, null for all.</param>
        Task<List<Usuario>> ObtenerTodosAsync(string? busqueda, bool? soloActivos);

        /// <summary>
        /// Gets a user by id (tracked: the service modifies it and then calls <see cref="GuardarCambiosAsync"/>).
        /// </summary>
        Task<Usuario?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Counts the active "General" accounts other than <paramref name="idExcluido"/>.
        /// Used to never leave the system without an active General administrator.
        /// </summary>
        Task<int> ContarOtrosGeneralesActivosAsync(int idExcluido);

        /// <summary>
        /// Saves the changes already applied to tracked users together with their audit rows (one transaction).
        /// Translates unique index violations (username / email) into a friendly message.
        /// </summary>
        Task<(bool Success, string Message)> GuardarCambiosAsync(IEnumerable<HistorialUsuario> historial);
    }
}
