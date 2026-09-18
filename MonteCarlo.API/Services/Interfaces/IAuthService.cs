using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces
{
    /// <summary>
    /// Provides authentication-related operations such as login and user creation.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Attempts to authenticate a user with the specified login request.
        /// </summary>
        /// <param name="request">The login request containing user credentials.</param>
        /// <returns>A <see cref="Result{LoginResponse}"/> containing authentication data and status.</returns>
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    }
}
