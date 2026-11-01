using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// Proceso automatico que cada minuto encola los recordatorios de reserva y envia los correos pendientes.
/// Corre mientras la API esta levantada.
public class NotificacionesHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificacionesHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notificacionesService = scope.ServiceProvider.GetRequiredService<INotificacionesService>();
                await notificacionesService.ProcesarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Un error en una vuelta no detiene el proceso: se reintenta en la siguiente.
                logger.LogError(ex, "Error al procesar las notificaciones de reservas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
