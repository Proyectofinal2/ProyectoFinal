using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>
/// Servicio de gestión de cuentas de administrador.
/// HU-AUT-006: consultar, desactivar y reactivar cuentas.
/// </summary>
public class UsuariosService(IUsuariosRepository usuariosRepository) : IUsuariosService
{
    
    /// <summary>
    /// Crea un nuevo usuario administrador.
    /// Valida duplicidades y hashea la contraseña.
    /// HU-AUT-005: crear cuenta de administrador.
    /// </summary>
    public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request)
    {

        // Crear entidad Usuario con datos del request
        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto,
            CorreoElectronico = request.CorreoElectronico,
            CorreoPersonal = request.CorreoPersonal,
            NombreUsuario = request.NombreUsuario,
            Rol = request.Rol,
            ContrasenaHash = HashearContrasena(request.ContrasenaInicial),
            Activo = true,
            IntentosFallidos = 0
        };

        // Crear en la base de datos
        var (success, message, createdUser) = await usuariosRepository.CrearUsuarioAsync(usuario);

        if (!success || createdUser == null)
        {
            // Si es error de duplicidad, retornar Conflict
            if (message.Contains("nombre de usuario") || message.Contains("correo"))
            {
                return Result<UserResponse>.Conflict(message);
            }
            return Result<UserResponse>.InternalError(message);
        }

        // Mapear a response
        var response = new UserResponse
        {
            IdUsuario = createdUser.IdUsuario,
            NombreCompleto = createdUser.NombreCompleto,
            NombreUsuario = createdUser.NombreUsuario,
            CorreoElectronico = createdUser.CorreoElectronico,
            CorreoPersonal = createdUser.CorreoPersonal,
            Rol = createdUser.Rol,
            Activo = createdUser.Activo
        };

        return Result<UserResponse>.Ok(response, "Cuenta creada exitosamente.");
    }
    
    /// <summary>
    /// Obtiene el listado de cuentas, filtrado opcionalmente por texto de búsqueda y estado.
    /// HU-AUT-006, escenario 1.
    /// </summary>
    public async Task<Result<List<UserResponse>>> ObtenerTodosAsync(string? busqueda, bool? soloActivos)
    {
        var usuarios = await usuariosRepository.ObtenerTodosAsync(busqueda, soloActivos);

        var response = usuarios.Select(MapearAUserResponse).ToList();

        return Result<List<UserResponse>>.Ok(response, "Listado obtenido exitosamente.");
    }

    /// <summary>
    /// Cambia el estado activo/inactivo de una cuenta.
    /// HU-AUT-006, escenarios 2, 3 y 4 (no se puede desactivar la propia cuenta).
    /// </summary>
    public async Task<Result<UserResponse>> CambiarEstadoAsync(int id, bool activo, int idUsuarioSolicitante)
    {
        if (!activo && id == idUsuarioSolicitante)
        {
            return Result<UserResponse>.BadRequest("No puedes desactivar la cuenta con la que iniciaste sesión.");
        }

        var (success, message) = await usuariosRepository.CambiarEstadoAsync(id, activo);

        if (!success)
        {
            return Result<UserResponse>.NotFound(message);
        }

        var usuario = await usuariosRepository.ObtenerPorIdAsync(id);
        if (usuario == null)
        {
            return Result<UserResponse>.NotFound("No se encontró la cuenta.");
        }

        return Result<UserResponse>.Ok(MapearAUserResponse(usuario), message);
    }

    private static UserResponse MapearAUserResponse(Data.Entities.Usuario usuario) => new()
    {
        IdUsuario = usuario.IdUsuario,
        NombreCompleto = usuario.NombreCompleto,
        NombreUsuario = usuario.NombreUsuario,
        CorreoElectronico = usuario.CorreoElectronico,
        CorreoPersonal = usuario.CorreoPersonal,
        Rol = usuario.Rol,
        Activo = usuario.Activo
    };


    /// <summary>
    /// BCrypt incluye protección automática contra timing attacks.
    /// Usa 12 rondas (recomendado por OWASP para máxima seguridad).
    /// </summary>
    private static string HashearContrasena(string contrasena)
    {
        // BCrypt.Net.BCrypt.HashPassword hashea con salt generado automáticamente
        // Usa 12 rondas por defecto (2^12 iteraciones)
        return BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12);
    }
}
