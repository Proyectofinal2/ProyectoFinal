using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces
{
    /// <summary>
    /// Provee operaciones de autenticación como el login y la recuperación de contraseña.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Intenta autenticar un usuario con la solicitud de login especificada.
        /// </summary>
        /// <param name="request">La solicitud de login con las credenciales del usuario.</param>
        /// <returns>Un <see cref="Result{LoginResponse}"/> con los datos de autenticación y el estado.</returns>
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// Solicita la recuperación de contraseña de un usuario. HU-AUT-003.
        /// Retorna un mensaje genérico de éxito para no revelar si el usuario existe
        /// o está inactivo. Esa ambigüedad no aplica a fallos reales de infraestructura
        /// (ej. el proveedor SMTP no responde): en ese caso retorna un error explícito.
        /// </summary>
        /// <param name="request">Solicitud con el usuario o correo del administrador.</param>
        Task<Result> SolicitarRecuperacionAsync(ForgotPasswordRequest request);

        /// <summary>
        /// Restablece la contraseña de un usuario usando un token de recuperación válido. HU-AUT-003.
        /// </summary>
        /// <param name="request">Solicitud con el token y la nueva contraseña.</param>
        Task<Result> RestablecerContrasenaAsync(ResetPasswordRequest request);

        /// <summary>
        /// Verifica si un token de recuperación es válido (no usado y no expirado),
        /// sin consumirlo. HU-AUT-003: permite a la UI avisar de un enlace roto antes
        /// de que el usuario llene el formulario.
        /// </summary>
        /// <param name="token">Token recibido por correo.</param>
        Task<Result> ValidarTokenRecuperacionAsync(string? token);
    }
}
