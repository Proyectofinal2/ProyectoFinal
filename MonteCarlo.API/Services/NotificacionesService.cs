using System.Globalization;
using System.Net;
using System.Net.Mail;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// Correos al cliente sobre su reserva: confirmacion, cancelacion y recordatorio.
/// Los correos se encolan como filas Pendiente en Notificacion y los envia NotificacionesHostedService,
/// asi un fallo o demora del servidor de correo no afecta el registro de la reserva.
public class NotificacionesService(
    INotificacionesRepository notificacionesRepository,
    IConfiguracionService configuracionService,
    IEmailService emailService,
    IConfiguration configuration,
    IWebHostEnvironment env,
    ILogger<NotificacionesService> logger) : INotificacionesService
{
    public const string Confirmacion = "Confirmacion";
    public const string Cancelacion = "Cancelacion";
    public const string Recordatorio = "Recordatorio";

    private const int LoteEnvio = 20;
    private const int IntentosPorLimite = 3;
    private static readonly TimeSpan EsperaPorLimite = TimeSpan.FromSeconds(10);
    private static readonly CultureInfo CulturaCr = new("es-CR");

    public void Encolar(Reserva reserva, string? correo, string tipoNotificacion)
    {
        if (reserva.EsWalkIn || string.IsNullOrWhiteSpace(correo))
            return;

        notificacionesRepository.Agregar(new Notificacion
        {
            Reserva = reserva,
            TipoNotificacion = tipoNotificacion,
            Canal = "Email",
            Destinatario = correo.Trim(),
            FechaCreacion = DateTime.Now,
            EstadoEnvio = "Pendiente"
        });
    }

    public async Task ProcesarAsync(CancellationToken cancellationToken = default)
    {
        await EncolarRecordatoriosAsync();
        await EnviarPendientesAsync(cancellationToken);
    }

    private async Task EncolarRecordatoriosAsync()
    {
        var horas = await configuracionService.ObtenerValorAnticipacionRecordatorioAsync();
        var ahora = DateTime.Now;
        var limite = ahora.AddHours(horas);

        var reservas = await notificacionesRepository.ObtenerReservasSinRecordatorioAsync(
            DateOnly.FromDateTime(ahora), DateOnly.FromDateTime(limite));

        foreach (var reserva in reservas)
        {
            var momento = reserva.FechaReserva.ToDateTime(reserva.HoraReserva);

            // Solo reservas que llegan dentro de la anticipacion configurada. Si se hizo cuando ya
            // faltaba menos que esa anticipacion, el correo de confirmacion es suficiente.
            if (momento <= ahora || momento > limite || reserva.FechaCreacion > momento.AddHours(-horas))
                continue;

            Encolar(reserva, reserva.Cliente.CorreoElectronico, Recordatorio);
        }

        await notificacionesRepository.GuardarCambiosAsync();
    }

    private async Task EnviarPendientesAsync(CancellationToken cancellationToken)
    {
        var pendientes = await notificacionesRepository.ObtenerPendientesAsync(LoteEnvio);
        if (pendientes.Count == 0)
            return;

        var rutaPlantilla = Path.Combine(env.ContentRootPath, "Templates", "NotificacionReservaTemplate.html");
        var plantilla = await File.ReadAllTextAsync(rutaPlantilla, cancellationToken);

        foreach (var notificacion in pendientes)
        {
            var reserva = notificacion.Reserva;

            if (notificacion.TipoNotificacion != Cancelacion && reserva.EstadoReserva.Nombre == "Cancelada")
            {
                // La reserva se cancelo antes de enviar este correo: ya no tiene sentido mandarlo.
                notificacion.EstadoEnvio = "Fallido";
                notificacion.DetalleError = "La reserva fue cancelada antes del envio.";
                await notificacionesRepository.GuardarCambiosAsync();
                continue;
            }

            var (asunto, cuerpo) = ConstruirCorreo(notificacion, plantilla);
            notificacion.Mensaje = $"{asunto} ({reserva.CodigoReserva})";

            if (!await EnviarConReintentoAsync(notificacion, asunto, cuerpo, cancellationToken))
            {
                // El servidor sigue limitando los envios: lo que falta queda Pendiente para la siguiente vuelta.
                logger.LogInformation("Limite de envios del servidor de correo alcanzado; se reintenta en el siguiente ciclo.");
                break;
            }

            // Se guarda cada una para no reenviar un correo si el proceso se detiene a mitad del lote.
            await notificacionesRepository.GuardarCambiosAsync();
        }
    }

    /// Envia el correo y deja la notificacion como Enviado o Fallido. Si el servidor responde que se supero
    /// su limite de envios (Mailtrap gratis acepta 1 correo cada 10 segundos), espera y reintenta.
    /// Devuelve false si despues de los reintentos el limite sigue activo; la notificacion queda Pendiente.
    private async Task<bool> EnviarConReintentoAsync(
        Notificacion notificacion, string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        for (var intento = 1; intento <= IntentosPorLimite; intento++)
        {
            try
            {
                await emailService.EnviarCorreoAsync(notificacion.Destinatario, asunto, cuerpo);
                notificacion.EstadoEnvio = "Enviado";
                notificacion.FechaEnvio = DateTime.Now;
                return true;
            }
            catch (SmtpException ex) when (ex.Message.Contains("Too many emails", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(EsperaPorLimite, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo enviar la notificacion {IdNotificacion} a {Destinatario}",
                    notificacion.IdNotificacion, notificacion.Destinatario);
                notificacion.EstadoEnvio = "Fallido";
                notificacion.DetalleError = ex.Message.Length > 300 ? ex.Message[..300] : ex.Message;
                return true;
            }
        }

        return false;
    }

    private (string Asunto, string Cuerpo) ConstruirCorreo(Notificacion notificacion, string plantilla)
    {
        var reserva = notificacion.Reserva;
        var baseUrl = (configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var enlaceConsulta = $"{baseUrl}/Reserva/Consultar?codigo={Uri.EscapeDataString(reserva.CodigoReserva ?? string.Empty)}";

        var (asunto, titulo, mensaje, textoEnlace, enlace) = notificacion.TipoNotificacion switch
        {
            Cancelacion => (
                "Reserva cancelada - Monte Carlo",
                "Reserva cancelada",
                MensajeCancelacion(reserva.MotivoCancelacion),
                "Hacer una nueva reserva",
                $"{baseUrl}/Reserva/Create"),
            Recordatorio => (
                "Recordatorio de tu reserva - Monte Carlo",
                "Recordatorio de tu reserva",
                "Te recordamos que tienes una reserva con nosotros. Si no puedes asistir, por favor cancela la reserva desde el enlace de abajo para liberar la mesa.",
                "Ver mi reserva",
                enlaceConsulta),
            _ when reserva.EstadoReserva.Nombre == "Pendiente" => (
                "Solicitud de reserva recibida - Monte Carlo",
                "Solicitud de reserva recibida",
                "Recibimos tu solicitud de reserva. Por la cantidad de personas debe ser revisada por nuestro equipo; te enviaremos otro correo cuando quede confirmada.",
                "Ver mi reserva",
                enlaceConsulta),
            _ => (
                "Reserva confirmada - Monte Carlo",
                "Reserva confirmada",
                "Confirmamos tu reserva. Te esperamos en la fecha y hora que se indican abajo.",
                "Ver mi reserva",
                enlaceConsulta)
        };

        var cuerpo = plantilla
            .Replace("{{TITULO}}", WebUtility.HtmlEncode(titulo))
            .Replace("{{NOMBRE}}", WebUtility.HtmlEncode(reserva.Cliente.Nombre))
            .Replace("{{MENSAJE}}", WebUtility.HtmlEncode(mensaje))
            .Replace("{{CODIGO}}", WebUtility.HtmlEncode(reserva.CodigoReserva ?? string.Empty))
            .Replace("{{FECHA}}", WebUtility.HtmlEncode(reserva.FechaReserva.ToString("dddd d 'de' MMMM 'de' yyyy", CulturaCr)))
            .Replace("{{HORA}}", reserva.HoraReserva.ToString("HH:mm"))
            .Replace("{{PERSONAS}}", reserva.CantidadPersonas.ToString())
            .Replace("{{TEXTO_ENLACE}}", WebUtility.HtmlEncode(textoEnlace))
            .Replace("{{ENLACE}}", enlace);

        return (asunto, cuerpo);
    }

    private static string MensajeCancelacion(string? motivoCancelacion) => motivoCancelacion switch
    {
        "Cancelada por el cliente" => "Tu reserva fue cancelada a solicitud tuya.",
        "Cancelada por el administrador" => "Tu reserva fue cancelada por el restaurante.",
        "Cierre administrativo" => "Tu reserva fue cancelada debido a un cierre del restaurante en esa fecha.",
        _ => "Tu reserva fue cancelada."
    } + " Si deseas, puedes hacer una nueva reserva desde nuestro sitio.";
}
