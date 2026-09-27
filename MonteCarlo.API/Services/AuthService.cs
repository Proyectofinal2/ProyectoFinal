using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>
/// Servicio de autenticación.
/// Maneja login, generación de tokens JWT, validación de credenciales y
/// recuperación de contraseña.
/// HU-AUT-001: inicio de sesión seguro. HU-AUT-003: recuperación de contraseña.
/// </summary>
public class AuthService(
    IAuthRepository authRepository,
    IConfiguration configuration,
    IEmailService emailService,
    IWebHostEnvironment env,
    ILogger<AuthService> logger) : IAuthService
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

    /// <summary>
    /// Solicita la recuperación de contraseña. HU-AUT-003.
    /// Si el usuario no existe o está inactivo, retorna el mismo mensaje genérico de
    /// éxito que un envío real, para no filtrar información sobre las cuentas del
    /// sistema. Esta ambigüedad aplica únicamente a la existencia/estado de la cuenta:
    /// un error real de infraestructura (ej. SMTP inalcanzable) sí retorna un error
    /// explícito, ya que en ese caso no hay información de cuentas que proteger y el
    /// administrador necesita saber que la solicitud no se pudo procesar.
    /// </summary>
    public async Task<Result> SolicitarRecuperacionAsync(ForgotPasswordRequest request)
    {
        var usuario = await authRepository.ObtenerPorNombreOCorreoAsync(request.UsuarioOCorreo);

        if (usuario == null)
        {
            logger.LogInformation(
                "Solicitud de recuperación de contraseña para usuario/correo inexistente: {UsuarioOCorreo}",
                request.UsuarioOCorreo);
            return ResultadoGenericoRecuperacion();
        }

        if (!usuario.Activo)
        {
            logger.LogWarning(
                "Solicitud de recuperación de contraseña para cuenta inactiva: IdUsuario={IdUsuario}",
                usuario.IdUsuario);
            return ResultadoGenericoRecuperacion();
        }

        await authRepository.InvalidarTokensActivosAsync(usuario.IdUsuario);

        var minutosExpiracion = ObtenerMinutosExpiracionRecuperacion();
        var tokenPlano = GenerarTokenAleatorio();
        var tokenHash = HashearToken(tokenPlano);
        var fechaExpiracion = DateTime.UtcNow.AddMinutes(minutosExpiracion);

        await authRepository.CrearTokenRecuperacionAsync(usuario.IdUsuario, tokenHash, fechaExpiracion);

        var destinatario = !string.IsNullOrWhiteSpace(usuario.CorreoPersonal)
            ? usuario.CorreoPersonal
            : usuario.CorreoElectronico;

        var baseUrl = configuration["Frontend:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Frontend:BaseUrl no está configurado en appsettings.");
        }

        var linkReset = $"{baseUrl.TrimEnd('/')}/admin/restablecer?token={Uri.EscapeDataString(tokenPlano)}";

        try
        {
            var cuerpoHtml = await RenderizarTemplateRecuperacionAsync(usuario.NombreCompleto, linkReset, minutosExpiracion);
            await emailService.EnviarCorreoAsync(destinatario, "Recuperación de contraseña - MonteCarlo", cuerpoHtml);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "No se pudo enviar el correo de recuperación de contraseña para IdUsuario={IdUsuario}",
                usuario.IdUsuario);
            return Result.InternalError();
        }

        return ResultadoGenericoRecuperacion();
    }

    /// <summary>
    /// Restablece la contraseña de un usuario usando un token de recuperación válido. HU-AUT-003.
    /// </summary>
    public async Task<Result> RestablecerContrasenaAsync(ResetPasswordRequest request)
    {
        var token = await BuscarTokenValidoAsync(request.Token);

        if (token == null)
        {
            return Result.BadRequest(MensajeTokenInvalido);
        }

        var nuevoHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaContrasena);

        await authRepository.RestablecerContrasenaAsync(token.Usuario, token, nuevoHash);

        return Result.Ok("Contraseña actualizada correctamente.");
    }

    /// <summary>
    /// Verifica si un token de recuperación es válido, sin consumirlo. HU-AUT-003.
    /// </summary>
    public async Task<Result> ValidarTokenRecuperacionAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.BadRequest(MensajeTokenInvalido);
        }

        var tokenValido = await BuscarTokenValidoAsync(token);

        return tokenValido != null
            ? Result.Ok("Token válido.")
            : Result.BadRequest(MensajeTokenInvalido);
    }

    private const string MensajeTokenInvalido = "El enlace de recuperación no es válido o ha expirado.";

    /// <summary>
    /// Hashea un token en texto plano y busca el token de recuperación válido
    /// (no usado, no expirado) correspondiente. Compartido por
    /// <see cref="RestablecerContrasenaAsync"/> y <see cref="ValidarTokenRecuperacionAsync"/>.
    /// </summary>
    private async Task<TokenRecuperacionContrasena?> BuscarTokenValidoAsync(string token)
    {
        var tokenHash = HashearToken(token);
        return await authRepository.ObtenerTokenValidoAsync(tokenHash);
    }

    /// <summary>
    /// Construye el resultado genérico de éxito de <see cref="SolicitarRecuperacionAsync"/>,
    /// idéntico exista o no la cuenta, para no revelar información sobre las cuentas del sistema.
    /// </summary>
    private static Result ResultadoGenericoRecuperacion()
        => Result.Ok("Si el usuario existe, se enviará un correo con instrucciones para restablecer la contraseña.");

    /// <summary>
    /// Lee "PasswordRecovery:ExpirationMinutes" de la configuración. Si no está definido o
    /// no es un número válido, usa 15 minutos como valor por defecto.
    /// </summary>
    private int ObtenerMinutosExpiracionRecuperacion()
    {
        var minutos = configuration.GetValue<int?>("PasswordRecovery:ExpirationMinutes");
        return minutos is > 0 ? minutos.Value : 15;
    }

    /// <summary>
    /// Genera un token de recuperación aleatorio criptográficamente seguro,
    /// codificado en Base64Url para uso seguro en URLs.
    /// </summary>
    private static string GenerarTokenAleatorio()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Calcula el hash determinista (SHA-256) de un token en texto plano, para
    /// poder buscarlo por igualdad en la base de datos sin almacenarlo en claro.
    /// </summary>
    private static string HashearToken(string tokenPlano)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenPlano));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Carga el template HTML de recuperación de contraseña y reemplaza sus placeholders.
    /// </summary>
    private async Task<string> RenderizarTemplateRecuperacionAsync(string nombre, string linkReset, int minutosExpiracion)
    {
        var rutaTemplate = Path.Combine(env.ContentRootPath, "Templates", "RecuperacionContrasenaTemplate.html");
        var template = await File.ReadAllTextAsync(rutaTemplate);

        return template
            .Replace("{{NOMBRE}}", WebUtility.HtmlEncode(nombre))
            .Replace("{{LINK_RESET}}", linkReset)
            .Replace("{{MINUTOS_EXPIRACION}}", minutosExpiracion.ToString());
    }
}
