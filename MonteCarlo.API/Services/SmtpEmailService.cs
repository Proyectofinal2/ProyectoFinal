using System.Net;
using System.Net.Mail;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>
/// Servicio de envío de correos vía SMTP.
/// Usado para notificaciones de recuperación de contraseña (HU-AUT-003).
/// </summary>
public class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    /// <summary>
    /// Envía un correo HTML usando la configuración "Smtp" de appsettings.
    /// </summary>
    public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var host = configuration["Smtp:Host"];
        var port = configuration.GetValue<int?>("Smtp:Port");
        var fromAddress = configuration["Smtp:FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || port is null or <= 0 || string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("La configuración de Smtp no está completa en appsettings.");
        }

        var enableSsl = configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true;
        var fromName = configuration["Smtp:FromName"] ?? fromAddress;
        var user = configuration["Smtp:User"];
        var password = configuration["Smtp:Password"];

        using var mensaje = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(host, port.Value)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(user))
        {
            cliente.Credentials = new NetworkCredential(user, password);
        }

        await cliente.SendMailAsync(mensaje);
    }
}
