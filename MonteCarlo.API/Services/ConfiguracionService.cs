using MonteCarlo.API.Data;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

// CAMBIO: Se agregó IMetodosPagoRepository al constructor
public class ConfiguracionService(
    IConfiguracionRepository configuracionRepository,
    IMetodosPagoRepository metodosPagoRepository) : IConfiguracionService
{
    // ============================================================
    // Claves de ConfiguracionSistema y sus valores por defecto.
    // ============================================================

    public const string ClaveUmbral = "UmbralConfirmacionAutomatica";
    private const int UmbralPorDefecto = 4;

    public const string ClaveDuracionReserva = "DuracionReservaMinutos";
    private const int DuracionReservaPorDefecto = 90;

    public const string ClaveVentanaAlertaReservaProxima = "VentanaAlertaReservaProximaMinutos";
    private const int VentanaAlertaReservaProximaPorDefecto = 30;

    public const string ClaveAnticipacionRecordatorio = "AnticipacionRecordatorioHoras";
    private const int AnticipacionRecordatorioPorDefecto = 24;

    public const string ClaveIntervaloCalendario = "IntervaloCalendarioDisponibilidadMinutos";
    private const int IntervaloCalendarioPorDefecto = 30;

    // ============================================================
    // Lectura usada por el resto del sistema
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
    // Pantalla dinamica de Parametros
    // ============================================================

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
            var clavesRecibidas = request.Parametros.Select(p => p.Clave).Distinct().ToArray();
            var porClave = (await configuracionRepository.ObtenerVariasAsync(clavesRecibidas)).ToDictionary(c => c.Clave);

            var errorValidacion = ValidarParametros(request.Parametros, porClave);
            if (errorValidacion is not null)
            {
                return Result<ConfiguracionGeneralResponse>.BadRequest(errorValidacion);
            }

            huboCambios = request.Parametros
                .DistinctBy(p => p.Clave)
                .Aggregate(false, (cambio, item) => cambio | ActualizarSiCambio(porClave, item.Clave, item.Valor, idUsuario));

            if (huboCambios)
            {
                await configuracionRepository.GuardarCambiosAsync();
            }
        }

        var configuracionesActuales = await configuracionRepository.ObtenerTodasAsync();

        return Result<ConfiguracionGeneralResponse>.Ok(
            MapearGeneral(configuracionesActuales),
            huboCambios
                ? "Los cambios se guardaron correctamente. Aplican solo a las reservas/recordatorios nuevos."
                : "No había cambios que guardar.");
    }

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

    // ============================================================
    // HU-CFG-007: Métodos de Pago
    // ============================================================

    public async Task<Result<List<MetodoPagoAdminResponse>>> ObtenerMetodosPagoAdminAsync()
    {
        var metodos = await metodosPagoRepository.ObtenerTodosAsync();
        return Result<List<MetodoPagoAdminResponse>>.Ok(metodos.Select(m => new MetodoPagoAdminResponse
        {
            IdMetodoPago = m.IdMetodoPago,
            Nombre = m.Nombre,
            Activo = m.Activo
        }).ToList(), "Métodos obtenidos.");
    }

    public async Task<Result<MetodoPagoAdminResponse>> CrearMetodoPagoAsync(CrearMetodoPagoRequest request)
    {
        if (await metodosPagoRepository.ExisteNombreAsync(request.Nombre))
            return Result<MetodoPagoAdminResponse>.BadRequest("Ya existe un método de pago con ese nombre.");

        var nuevo = new MetodoPago { Nombre = request.Nombre, Activo = true };
        metodosPagoRepository.Agregar(nuevo);
        await metodosPagoRepository.GuardarCambiosAsync();

        return Result<MetodoPagoAdminResponse>.Ok(new MetodoPagoAdminResponse
        {
            IdMetodoPago = nuevo.IdMetodoPago,
            Nombre = nuevo.Nombre,
            Activo = nuevo.Activo
        }, "Método de pago creado.");
    }

    public async Task<Result> CambiarEstadoMetodoPagoAsync(int id, bool activo)
    {
        var metodo = await metodosPagoRepository.ObtenerPorIdAsync(id);
        if (metodo is null) return Result.BadRequest("Método de pago no encontrado.");

        metodo.Activo = activo;
        await metodosPagoRepository.GuardarCambiosAsync();
        return Result.Ok(activo ? "Método habilitado." : "Método deshabilitado.");
    }
}