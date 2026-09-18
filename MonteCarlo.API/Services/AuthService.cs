using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>
/// Servicio de autenticación.
/// Maneja login, generación de tokens JWT y validación de credenciales.
/// HU-AUT-001: inicio de sesión seguro.
/// </summary>
public class AuthService(IAuthRepository authRepository, IConfiguration configuration): IAuthService
{

    /// <summary>
    /// Intenta autenticar un usuario con sus credenciales.
    /// Retorna un Result con LoginResponse si es exitoso.
    /// HU-AUT-001: maneja los 3 escenarios (éxito, credenciales inválidas, bloqueo).
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        // Buscar usuario por nombre de usuario o correo
        var usuario = await authRepository.ObtenerPorNombreOCorreoAsync(request.Usuario);

        // HU-AUT-001, escenario 2: no revelar si existe o no (seguridad)
        if (usuario == null)
        {
            return Result<LoginResponse>.Unauthorized("Usuario o contraseña incorrectos.");
        }

        // Verificar si está inactivo
        if (!usuario.Activo)
        {
            return Result<LoginResponse>.Unauthorized("Esta cuenta ha sido desactivada.");
        }

        // HU-AUT-001, escenario 3: verificar bloqueo temporal
        if (authRepository.EstaBloqueadoTemporalmente(usuario))
        {
            var segundosRestantes = (int)Math.Ceiling(
                (usuario.FechaBloqueoHasta.GetValueOrDefault() - DateTime.UtcNow).TotalSeconds);
            var minutosRestantes = (int)Math.Ceiling(segundosRestantes / 60.0);
            return Result<LoginResponse>.Unauthorized(
                $"Cuenta temporalmente bloqueada. Intenta nuevamente en {minutosRestantes} minuto(s).",
                errorData: new { SegundosRestantes = Math.Max(segundosRestantes, 0) });
        }

        // Verificar contraseña
        if (!VerificarContrasena(request.Contrasena, usuario.ContrasenaHash))
        {
            // Registrar intento fallido
            await authRepository.RegistrarIntentoFallidoAsync(usuario);
            return Result<LoginResponse>.Unauthorized("Usuario o contraseña incorrectos.");
        }

        // HU-AUT-001, escenario 1: login exitoso
        // Resetear intentos fallidos
        await authRepository.RestablecerIntentosFallidosAsync(usuario);

        // Generar token JWT
        var expirationMinutes = ObtenerMinutosExpiracion();
        var token = GenerarTokenJwt(usuario, expirationMinutes);

        var response = new LoginResponse
        {
            IdUsuario = usuario.IdUsuario,
            NombreCompleto = usuario.NombreCompleto,
            CorreoElectronico = usuario.CorreoElectronico,
            Rol = usuario.Rol,
            Token = token,
            ExpiresIn = expirationMinutes * 60
        };

        return Result<LoginResponse>.Ok(response, "Inicio de sesión exitoso.");
    }

    /// <summary>
    /// Lee "Jwt:ExpirationMinutes" de la configuración. Si no está definido o no es
    /// un número válido, usa 60 minutos como valor por defecto.
    /// </summary>
    private int ObtenerMinutosExpiracion()
    {
        var minutos = configuration.GetValue<int?>("Jwt:ExpirationMinutes");
        return minutos is > 0 ? minutos.Value : 60;
    }

    /// <summary>
    /// Genera un token JWT firmado para el usuario.
    /// Incluye claims con ID, nombre, correo y rol.
    /// </summary>
    private string GenerarTokenJwt(Usuario usuario, int expirationMinutes)
    {
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException("Jwt:Secret no está configurado en appsettings.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                usuario.IdUsuario.ToString()),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Name,
                usuario.NombreUsuario),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Email,
                usuario.CorreoElectronico),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Role,
                usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Verifica una contraseña en texto plano contra su hash BCrypt.
    /// BCrypt protege automáticamente contra timing attacks.
    /// </summary>
    private static bool VerificarContrasena(string contrasenaplano, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(contrasenaplano, hash);
        }
        catch
        {
            return false;
        }
    }
}
