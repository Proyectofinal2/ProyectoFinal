using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IConfiguracionService
{
    Task<Result<UmbralConfirmacionResponse>> ObtenerUmbralAsync();

    Task<Result<UmbralConfirmacionResponse>> ActualizarUmbralAsync(int umbral, int idUsuario);


    /// Umbral vigente. El flujo de creación de reservas (HU-RES-003) debe leerlo
    /// en el momento de crear la reserva: así un cambio solo afecta a las nuevas.

    Task<int> ObtenerValorUmbralAsync();
}
