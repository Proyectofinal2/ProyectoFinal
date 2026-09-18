using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

/// <summary>
/// Servicio de gestión de cuentas de administrador.
/// HU-AUT-006: consultar, desactivar y reactivar cuentas.
/// </summary>
public interface IUsuariosService
{
    /// <summary>
    /// Obtiene el listado de cuentas, filtrado opcionalmente por texto de búsqueda y estado.
    /// </summary>
    Task<Result<List<UserResponse>>> ObtenerTodosAsync(string? busqueda, bool? soloActivos);

    /// <summary>
    /// Cambia el estado activo/inactivo de una cuenta.
    /// </summary>
    /// <param name="id">Id de la cuenta a modificar.</param>
    /// <param name="activo">True para reactivar, false para desactivar.</param>
    /// <param name="idUsuarioSolicitante">Id de la cuenta que realiza la solicitud (no puede desactivarse a sí misma).</param>
    Task<Result<UserResponse>> CambiarEstadoAsync(int id, bool activo, int idUsuarioSolicitante);

    /// <summary>
    /// Creates a new user with the specified information.
    /// </summary>
    /// <param name="request">The request containing new user details.</param>
    /// <returns>A <see cref="Result{UserResponse}"/> containing the created user data and status.</returns>
    Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request);
}
