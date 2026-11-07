using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>
/// Servicio de gestión de cuentas de administrador.
/// HU-AUT-006: consultar, desactivar y reactivar cuentas.
/// HU-AUT-007: editar cuentas; toda alta, edición y cambio de estado queda en el historial con su responsable.
/// </summary>
public class UsuariosService(IUsuariosRepository usuariosRepository) : IUsuariosService
{
    private const string General = "General";

    /// <summary>
    /// Crea un nuevo usuario administrador.
    /// Valida duplicidades y hashea la contraseña.
    /// HU-AUT-005: crear cuenta de administrador.
    /// </summary>
    public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, int idUsuarioSolicitante)
    {
        // Crear entidad Usuario con datos del request
        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            CorreoElectronico = request.CorreoElectronico.Trim(),
            CorreoPersonal = NormalizarOpcional(request.CorreoPersonal),
            NombreUsuario = request.NombreUsuario.Trim(),
            Rol = request.Rol,
            ContrasenaHash = HashearContrasena(request.ContrasenaInicial),
            Activo = true,
            IntentosFallidos = 0
        };

        // Crear en la base de datos (cuenta + historial en el mismo guardado)
        var historial = HistorialUsuarioFactory.Creada(usuario, idUsuarioSolicitante);
        var (success, message, createdUser) = await usuariosRepository.CrearUsuarioAsync(usuario, historial);

        if (!success || createdUser == null)
        {
            return ErrorDeGuardado<UserResponse>(message);
        }

        return Result<UserResponse>.Ok(MapearAUserResponse(createdUser), "Cuenta creada exitosamente.");
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

    public async Task<Result<UserResponse>> ObtenerPorIdAsync(int id)
    {
        var usuario = await usuariosRepository.ObtenerPorIdAsync(id);

        return usuario is null
            ? Result<UserResponse>.NotFound("No se encontró la cuenta.")
            : Result<UserResponse>.Ok(MapearAUserResponse(usuario), "Cuenta obtenida exitosamente.");
    }

    /// <summary>
    /// Cambia el estado activo/inactivo de una cuenta.
    /// HU-AUT-006, escenarios 2, 3 y 4 (no se puede desactivar la propia cuenta ni la última General activa).
    /// HU-AUT-007: deja en el historial quién lo hizo, cuándo y el motivo.
    /// </summary>
    public async Task<Result<UserResponse>> CambiarEstadoAsync(int id, bool activo, int idUsuarioSolicitante, string? motivo = null)
    {
        if (!activo && id == idUsuarioSolicitante)
        {
            return Result<UserResponse>.BadRequest("No puedes desactivar la cuenta con la que iniciaste sesión.");
        }

        var usuario = await usuariosRepository.ObtenerPorIdAsync(id);
        if (usuario == null)
        {
            return Result<UserResponse>.NotFound("No se encontró la cuenta.");
        }

        // Ya estaba en ese estado: no hay cambio, no hay historial.
        if (usuario.Activo == activo)
        {
            return Result<UserResponse>.Ok(MapearAUserResponse(usuario),
                activo ? "La cuenta ya estaba activa." : "La cuenta ya estaba desactivada.");
        }

        if (!activo && usuario.Rol == General && await usuariosRepository.ContarOtrosGeneralesActivosAsync(id) == 0)
        {
            return Result<UserResponse>.Conflict("No se puede desactivar la última cuenta General activa.");
        }

        usuario.Activo = activo;
        var historial = HistorialUsuarioFactory.Estado(usuario, idUsuarioSolicitante, activo, motivo);

        var (success, message) = await usuariosRepository.GuardarCambiosAsync([historial]);
        if (!success)
        {
            return ErrorDeGuardado<UserResponse>(message);
        }

        return Result<UserResponse>.Ok(MapearAUserResponse(usuario),
            activo ? "La cuenta fue reactivada." : "La cuenta fue desactivada.");
    }

    /// <summary>
    /// Edita una cuenta (HU-AUT-007). Una fila de historial por campo realmente cambiado,
    /// guardada en la misma transacción que la edición.
    /// </summary>
    public async Task<Result<UserResponse>> ActualizarAsync(int id, UpdateUserRequest request, int idUsuarioSolicitante)
    {
        var usuario = await usuariosRepository.ObtenerPorIdAsync(id);
        if (usuario == null)
        {
            return Result<UserResponse>.NotFound("No se encontró la cuenta.");
        }

        var nombreCompleto = request.NombreCompleto.Trim();
        var nombreUsuario = request.NombreUsuario.Trim();
        var correo = request.CorreoElectronico.Trim();
        var correoPersonal = NormalizarOpcional(request.CorreoPersonal);
        var rol = request.Rol;

        if (rol != usuario.Rol)
        {
            if (id == idUsuarioSolicitante)
            {
                return Result<UserResponse>.BadRequest("No puedes cambiar el rol de la cuenta con la que iniciaste sesión.");
            }

            if (usuario.Rol == General && usuario.Activo && await usuariosRepository.ContarOtrosGeneralesActivosAsync(id) == 0)
            {
                return Result<UserResponse>.Conflict("No se puede quitar el rol General a la última cuenta General activa.");
            }
        }

        // Valores anteriores y nuevos, campo por campo; solo se registran los que cambian.
        var historial = new List<HistorialUsuario>();
        void Comparar(string campo, string? anterior, string? nuevo, Action aplicar)
        {
            if (string.Equals(anterior, nuevo, StringComparison.Ordinal)) return;
            historial.Add(HistorialUsuarioFactory.Editada(usuario, idUsuarioSolicitante, campo, anterior, nuevo));
            aplicar();
        }

        Comparar(HistorialUsuarioCampos.NombreCompleto, usuario.NombreCompleto, nombreCompleto, () => usuario.NombreCompleto = nombreCompleto);
        Comparar(HistorialUsuarioCampos.NombreUsuario, usuario.NombreUsuario, nombreUsuario, () => usuario.NombreUsuario = nombreUsuario);
        Comparar(HistorialUsuarioCampos.CorreoElectronico, usuario.CorreoElectronico, correo, () => usuario.CorreoElectronico = correo);
        Comparar(HistorialUsuarioCampos.CorreoPersonal, usuario.CorreoPersonal, correoPersonal, () => usuario.CorreoPersonal = correoPersonal);
        Comparar(HistorialUsuarioCampos.Rol, usuario.Rol, rol, () => usuario.Rol = rol);

        if (historial.Count == 0)
        {
            return Result<UserResponse>.Ok(MapearAUserResponse(usuario), "No hay cambios que guardar.");
        }

        var (success, message) = await usuariosRepository.GuardarCambiosAsync(historial);
        if (!success)
        {
            return ErrorDeGuardado<UserResponse>(message);
        }

        return Result<UserResponse>.Ok(MapearAUserResponse(usuario), "Cuenta actualizada exitosamente.");
    }

    private static Result<T> ErrorDeGuardado<T>(string message) =>
        message.Contains("nombre de usuario") || message.Contains("correo")
            ? Result<T>.Conflict(message)
            : Result<T>.InternalError(message);

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static UserResponse MapearAUserResponse(Usuario usuario) => new()
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
