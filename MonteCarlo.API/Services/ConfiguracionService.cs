using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class ConfiguracionService(IConfiguracionRepository configuracionRepository) : IConfiguracionService
{
    public const string ClaveUmbral = "UmbralConfirmacionAutomatica";
    private const int UmbralPorDefecto = 4;

    public async Task<int> ObtenerValorUmbralAsync()
    {
        var config = await configuracionRepository.ObtenerAsync(ClaveUmbral);
        return LeerUmbral(config);
    }

    public async Task<Result<UmbralConfirmacionResponse>> ObtenerUmbralAsync()
    {
        var config = await configuracionRepository.ObtenerAsync(ClaveUmbral);
        return Result<UmbralConfirmacionResponse>.Ok(Mapear(config), "Umbral obtenido exitosamente.");
    }

    public async Task<Result<UmbralConfirmacionResponse>> ActualizarUmbralAsync(int umbral, int idUsuario)
    {
        if (umbral <= 0)
        {
            return Result<UmbralConfirmacionResponse>.BadRequest("El umbral debe ser un número entero positivo.");
        }

        var config = await configuracionRepository.ObtenerAsync(ClaveUmbral);

        if (config is null)
        {
            config = new ConfiguracionSistema { Clave = ClaveUmbral };
            configuracionRepository.Agregar(config);
        }

        // Fecha y usuario del cambio (HU-CFG-001). Las reservas existentes conservan su
        // estado: el umbral solo se evalúa al crear una reserva nueva.
        config.Valor = umbral.ToString();
        config.FechaModificacion = DateTime.Now;
        config.IdUsuarioModificacion = idUsuario;

        await configuracionRepository.GuardarCambiosAsync();

        // Recargar para traer el nombre del usuario que hizo el cambio.
        config = await configuracionRepository.ObtenerAsync(ClaveUmbral);

        return Result<UmbralConfirmacionResponse>.Ok(
            Mapear(config),
            $"El umbral se actualizó a {umbral}. Aplica solo a las reservas nuevas.");
    }

    private static int LeerUmbral(ConfiguracionSistema? config) =>
        config is not null && int.TryParse(config.Valor, out var valor) && valor > 0
            ? valor
            : UmbralPorDefecto;

    private static UmbralConfirmacionResponse Mapear(ConfiguracionSistema? config) => new()
    {
        Umbral = LeerUmbral(config),
        FechaModificacion = config?.FechaModificacion,
        UsuarioModificacion = config?.UsuarioModificacion?.NombreCompleto
    };
}
