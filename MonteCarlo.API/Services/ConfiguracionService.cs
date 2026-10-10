using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class ConfiguracionService(IConfiguracionRepository configuracionRepository) : IConfiguracionService
{
    // ============================================================
    // Claves de ConfiguracionSistema y sus valores por defecto.
    // ============================================================

    public const string ClaveUmbral = "UmbralConfirmacionAutomatica";
    private const int UmbralPorDefecto = 4;

    /// <summary>Duracion estimada de un turno/reserva (HU-CFG-006), usada para
    /// solapamiento y disponibilidad.</summary>
    public const string ClaveDuracionReserva = "DuracionReservaMinutos";
    private const int DuracionReservaPorDefecto = 90;

    /// <summary>Ventana de bloqueo para no asignar un walk-in a una mesa con una
    /// reserva confirmada por llegar. Independiente de ClaveDuracionReserva.</summary>
    public const string ClaveVentanaAlertaReservaProxima = "VentanaAlertaReservaProximaMinutos";
    private const int VentanaAlertaReservaProximaPorDefecto = 30;

    /// <summary>Horas de anticipacion con que se envia el recordatorio automatico
    /// al cliente antes de su reserva (HU-CFG-006 / HU-NOT-003).</summary>
    public const string ClaveAnticipacionRecordatorio = "AnticipacionRecordatorioHoras";
    private const int AnticipacionRecordatorioPorDefecto = 24;

    /// <summary>Intervalo entre horarios de llegada ofrecidos en el calendario de
    /// disponibilidad (HU-RES-002), ej. cada 30 min: 12:00, 12:30, 13:00...</summary>
    public const string ClaveIntervaloCalendario = "IntervaloCalendarioDisponibilidadMinutos";
    private const int IntervaloCalendarioPorDefecto = 30;

    // ============================================================
    // Lectura usada por el resto del sistema (solapamiento, bloqueo
    // de walk-in, recordatorios) para leer un parametro vigente.
    // ============================================================

    public Task<int> ObtenerValorUmbralAsync() =>
        ObtenerValorEnteroAsync(ClaveUmbral, UmbralPorDefecto);

    public Task<int> ObtenerValorDuracionReservaAsync() =>
        ObtenerValorEnteroAsync(ClaveDuracionReserva, DuracionReservaPorDefecto);

    public Task<int> ObtenerValorVentanaAlertaReservaProximaAsync() =>
        ObtenerValorEnteroAsync(ClaveVentanaAlertaReservaProxima, VentanaAlertaReservaProximaPorDefecto);

    public Task<int> ObtenerValorAnticipacionRecordatorioAsync() =>
        ObtenerValorEnteroAsync(ClaveAnticipacionRecordatorio, AnticipacionRecordatorioPorDefecto);

    public Task<int> ObtenerValorIntervaloCalendarioAsync() =>
        ObtenerValorEnteroAsync(ClaveIntervaloCalendario, IntervaloCalendarioPorDefecto);

    private async Task<int> ObtenerValorEnteroAsync(string clave, int porDefecto)
    {
        var config = await configuracionRepository.ObtenerAsync(clave);
        return LeerEntero(config, porDefecto);
    }

    private static int LeerEntero(ConfiguracionSistema? config, int porDefecto) =>
        config is not null && int.TryParse(config.Valor, out var valor) && valor > 0
            ? valor
            : porDefecto;

    // ============================================================
    // Pantalla dinamica de Parametros: muestra TODO lo que exista en
    // ConfiguracionSistema y guarda solo lo que cambió (PATCH simplificado).
    // ============================================================

    /// <summary>Rango válido (min/max) de las claves numéricas conocidas. Es la única
    /// metadata que sigue en código (regla de negocio); Etiqueta/Descripcion ya
    /// viven en la base de datos. Una clave sin rango solo exige no venir vacía.</summary>
    private static readonly Dictionary<string, (int Min, int Max)> RangosParametros = new()
    {
        [ClaveUmbral] = (1, 1000),
        [ClaveDuracionReserva] = (1, 1440),
        [ClaveVentanaAlertaReservaProxima] = (1, 1440),
        [ClaveAnticipacionRecordatorio] = (1, 168),
        [ClaveIntervaloCalendario] = (5, 180),
    };

    public async Task<Result<ConfiguracionGeneralResponse>> ObtenerConfiguracionGeneralAsync()
    {
        var configuraciones = await configuracionRepository.ObtenerTodasAsync();
        return Result<ConfiguracionGeneralResponse>.Ok(MapearGeneral(configuraciones), "Configuración obtenida exitosamente.");
    }

    public async Task<Result<ConfiguracionGeneralResponse>> ActualizarConfiguracionGeneralAsync(
        ActualizarConfiguracionGeneralRequest request, int idUsuario)
    {
        var huboCambios = false;

        if (request.Parametros.Count > 0)
        {
            // Solo se consultan las claves recibidas: si el admin cambió 1 de 10
            // parámetros, no hace falta traer los otros 9 para aplicar el patch.
            var clavesRecibidas = request.Parametros.Select(p => p.Clave).Distinct().ToArray();
            var porClave = (await configuracionRepository.ObtenerVariasAsync(clavesRecibidas)).ToDictionary(c => c.Clave);

            var errorValidacion = ValidarParametros(request.Parametros, porClave);
            if (errorValidacion is not null)
            {
                return Result<ConfiguracionGeneralResponse>.BadRequest(errorValidacion);
            }

            // Se usa Aggregate con "|" (no "||") a propósito: deben aplicarse todos
            // los parámetros recibidos aunque alguno anterior ya haya cambiado.
            huboCambios = request.Parametros
                .DistinctBy(p => p.Clave)
                .Aggregate(false, (cambio, item) => cambio | ActualizarSiCambio(porClave, item.Clave, item.Valor, idUsuario));

            if (huboCambios)
            {
                await configuracionRepository.GuardarCambiosAsync();
            }
        }

        // Recargar el set completo (no solo lo tocado) para repintar toda la
        // pantalla, trayendo además el nombre del usuario que modificó cada uno.
        var configuracionesActuales = await configuracionRepository.ObtenerTodasAsync();

        return Result<ConfiguracionGeneralResponse>.Ok(
            MapearGeneral(configuracionesActuales),
            huboCambios
                ? "Los cambios se guardaron correctamente. Aplican solo a las reservas/recordatorios nuevos."
                : "No había cambios que guardar.");
    }

    /// <summary>Valida valor no vacío y, si la clave tiene rango conocido, que esté
    /// dentro de él. Devuelve el primer error, o null si todos son válidos.</summary>
    private static string? ValidarParametros(
        List<ActualizarParametroItem> parametros, Dictionary<string, ConfiguracionSistema> porClave)
    {
        foreach (var item in parametros)
        {
            if (string.IsNullOrWhiteSpace(item.Valor))
            {
                return $"'{item.Clave}' no puede quedar vacío.";
            }

            if (!RangosParametros.TryGetValue(item.Clave, out var rango))
            {
                continue;
            }

            if (!int.TryParse(item.Valor, out var valor) || valor < rango.Min || valor > rango.Max)
            {
                var etiqueta = porClave.GetValueOrDefault(item.Clave)?.Etiqueta ?? item.Clave;
                return $"{etiqueta} debe ser un número entero entre {rango.Min} y {rango.Max}.";
            }
        }

        return null;
    }

    /// <summary>Actualiza el valor en memoria solo si cambió respecto al actual (o si
    /// el parámetro no existía), para no pisar la fecha/usuario de los que no se tocaron.
    /// Devuelve true si el parámetro quedó marcado para guardar.</summary>
    private bool ActualizarSiCambio(Dictionary<string, ConfiguracionSistema> porClave, string clave, string valor, int idUsuario)
    {
        if (!porClave.TryGetValue(clave, out var config))
        {
            config = new ConfiguracionSistema { Clave = clave };
            configuracionRepository.Agregar(config);
            porClave[clave] = config;
        }
        else if (config.Valor == valor)
        {
            return false;
        }

        config.Valor = valor;
        config.FechaModificacion = DateTime.Now;
        config.IdUsuarioModificacion = idUsuario;
        return true;
    }

    private static ConfiguracionGeneralResponse MapearGeneral(List<ConfiguracionSistema> configuraciones) => new()
    {
        Parametros = configuraciones.Select(c => new ParametroConfiguracionResponse
        {
            Clave = c.Clave,
            Etiqueta = string.IsNullOrWhiteSpace(c.Etiqueta) ? c.Clave : c.Etiqueta,
            Descripcion = c.Descripcion,
            Valor = c.Valor,
            FechaModificacion = c.FechaModificacion,
            UsuarioModificacion = c.UsuarioModificacion?.NombreCompleto
        }).ToList()
    };
}
