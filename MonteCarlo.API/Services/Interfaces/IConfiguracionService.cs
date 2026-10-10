using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IConfiguracionService
{
    /// Umbral vigente. El flujo de creación de reservas (HU-RES-003) debe leerlo
    /// en el momento de crear la reserva: así un cambio solo afecta a las nuevas.
    Task<int> ObtenerValorUmbralAsync();

    /// Duración (en minutos) usada para detectar solapamiento entre reservas de una
    /// misma mesa y para el cálculo de disponibilidad del calendario (HU-RES-002).
    Task<int> ObtenerValorDuracionReservaAsync();

    /// Ventana (en minutos) usada para bloquear la asignación de un walk-in a una mesa
    /// con una reserva confirmada por llegar (independiente de la duración del turno).
    Task<int> ObtenerValorVentanaAlertaReservaProximaAsync();

    /// Horas de anticipación con que se envía el recordatorio automático al cliente
    /// antes de su reserva (HU-CFG-006 / HU-NOT-003).
    Task<int> ObtenerValorAnticipacionRecordatorioAsync();

    /// Intervalo (en minutos) entre horarios de llegada ofrecidos en el calendario
    /// de disponibilidad (HU-RES-002).
    Task<int> ObtenerValorIntervaloCalendarioAsync();

    /// Todos los parametros existentes en ConfiguracionSistema en una sola llamada
    /// (no un set fijo de claves), para la pantalla dinamica de Parametros (un solo
    /// GET, un solo PUT con un unico "Guardar cambios").
    Task<Result<ConfiguracionGeneralResponse>> ObtenerConfiguracionGeneralAsync();

    Task<Result<ConfiguracionGeneralResponse>> ActualizarConfiguracionGeneralAsync(
        ActualizarConfiguracionGeneralRequest request, int idUsuario);
}
