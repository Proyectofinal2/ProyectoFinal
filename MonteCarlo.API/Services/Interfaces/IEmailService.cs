namespace MonteCarlo.API.Services.Interfaces;

/// <summary>
/// Provee capacidades de envío de correos (ej. notificaciones de recuperación de contraseña).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo HTML al destinatario especificado.
    /// </summary>
    /// <param name="destinatario">Dirección de correo del destinatario.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpoHtml">Cuerpo HTML del correo.</param>
    Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml);
}
