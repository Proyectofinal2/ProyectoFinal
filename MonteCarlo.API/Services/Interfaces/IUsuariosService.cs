using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

/// <summary>
/// Servicio de gestión de cuentas de administrador.
/// HU-AUT-006: consultar, desactivar y reactivar cuentas. HU-AUT-007: editar cuentas con historial.
/// </summary>
public interface IUsuariosService
{
    /// <summary>
    /// Obtiene el listado de cuentas, filtrado opcionalmente por texto de búsqueda y estado.
    /// </summary>
    Task<Result<List<UserResponse>>> ObtenerTodosAsync(string? busqueda, bool? soloActivos);

    /// <summary>Obtiene una cuenta por id.</summary>
    Task<Result<UserResponse>> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Cambia el estado activo/inactivo de una cuenta y lo registra en el historial.
    /// </summary>
    /// <param name="id">Id de la cuenta a modificar.</param>
    /// <param name="activo">True para reactivar, false para desactivar.</param>
    /// <param name="idUsuarioSolicitante">Id de la cuenta que realiza la solicitud (no puede desactivarse a sí misma).</param>
    /// <param name="motivo">Motivo opcional que queda en el historial.</param>
    Task<Result<UserResponse>> CambiarEstadoAsync(int id, bool activo, int idUsuarioSolicitante, string? motivo = null);

    /// <summary>
    /// Edita nombre, usuario, correos y rol de una cuenta. Registra en el historial un cambio por campo.
    /// </summary>
    Task<Result<UserResponse>> ActualizarAsync(int id, UpdateUserRequest request, int idUsuarioSolicitante);

    /// <summary>
    /// Creates a new user with the specified information and records who created it.
    /// </summary>
    /// <param name="request">The request containing new user details.</param>
    /// <param name="idUsuarioSolicitante">Administrator creating the account.</param>
    /// <returns>A <see cref="Result{UserResponse}"/> containing the created user data and status.</returns>
    Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, int idUsuarioSolicitante);
}
