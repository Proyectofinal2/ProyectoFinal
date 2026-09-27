using MonteCarlo.API.Data.Entities;
using System.Threading.Tasks;

namespace MonteCarlo.API.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio para operaciones de autenticación y de usuarios.
    /// </summary>
    public interface IAuthRepository
    {
        /// <summary>
        /// Obtiene un usuario por nombre de usuario o correo electrónico.
        /// </summary>
        /// <param name="usuarioOCorreo">Nombre de usuario o correo a buscar.</param>
        /// <returns>El <see cref="Usuario"/> encontrado; de lo contrario null.</returns>
        Task<Usuario?> ObtenerPorNombreOCorreoAsync(string usuarioOCorreo);

        /// <summary>
        /// Registra un intento fallido de login para el usuario especificado.
        /// </summary>
        /// <param name="usuario">El usuario a actualizar.</param>
        Task RegistrarIntentoFallidoAsync(Usuario usuario);

        /// <summary>
        /// Resetea los intentos fallidos de login del usuario especificado.
        /// </summary>
        /// <param name="usuario">El usuario a actualizar.</param>
        Task RestablecerIntentosFallidosAsync(Usuario usuario);

        /// <summary>
        /// Determina si el usuario está temporalmente bloqueado.
        /// </summary>
        /// <param name="usuario">El usuario a verificar.</param>
        /// <returns>True si el usuario está temporalmente bloqueado; de lo contrario false.</returns>
        bool EstaBloqueadoTemporalmente(Usuario usuario);

        /// <summary>
        /// Invalida (marca como utilizados) todos los tokens de recuperación
        /// activos (no usados y no expirados) de un usuario. HU-AUT-003.
        /// </summary>
        /// <param name="idUsuario">Id del usuario.</param>
        Task InvalidarTokensActivosAsync(int idUsuario);

        /// <summary>
        /// Crea y persiste un nuevo token de recuperación de contraseña. HU-AUT-003.
        /// </summary>
        /// <param name="idUsuario">Id del usuario.</param>
        /// <param name="tokenHash">Hash determinista (SHA-256) del token en texto plano.</param>
        /// <param name="fechaExpiracion">Fecha y hora (UTC) de expiración del token.</param>
        /// <returns>El token de recuperación creado.</returns>
        Task<TokenRecuperacionContrasena> CrearTokenRecuperacionAsync(int idUsuario, string tokenHash, DateTime fechaExpiracion);

        /// <summary>
        /// Obtiene un token de recuperación válido (no usado y no expirado) por su hash. HU-AUT-003.
        /// </summary>
        /// <param name="tokenHash">Hash determinista (SHA-256) del token en texto plano.</param>
        /// <returns>El token con su usuario incluido si es válido; de lo contrario null.</returns>
        Task<TokenRecuperacionContrasena?> ObtenerTokenValidoAsync(string tokenHash);

        /// <summary>
        /// Actualiza la contraseña de un usuario, limpia su estado de bloqueo y marca el
        /// token de recuperación utilizado como utilizado, todo en una única transacción. HU-AUT-003.
        /// </summary>
        /// <param name="usuario">El usuario a actualizar.</param>
        /// <param name="token">El token de recuperación utilizado.</param>
        /// <param name="nuevoHash">Hash BCrypt de la nueva contraseña.</param>
        Task RestablecerContrasenaAsync(Usuario usuario, TokenRecuperacionContrasena token, string nuevoHash);
    }
}
