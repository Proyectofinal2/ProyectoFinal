using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HorariosOperacionService(
    IHorariosOperacionRepository horariosOperacionRepository,
    IHistorialHorarioOperacionRepository historialRepository,
    ICierresFijosService cierresFijosService) : IHorariosOperacionService
{
    public async Task<Result<List<HorarioOperacionResponse>>> ObtenerAsync()
    {
        var horarios = await horariosOperacionRepository.ObtenerTodosAsync();
        var respuesta = horarios.GroupBy(h => h.DiaSemana).Select(g => Mapear(g.First())).ToList();
        return Result<List<HorarioOperacionResponse>>.Ok(respuesta, "Horarios de operación obtenidos exitosamente.");
    }

    public async Task<Result<HorarioOperacionResponse>> GuardarAsync(
        int? diaSemana, TimeOnly? horaApertura, TimeOnly? horaCierre, int idUsuario)
    {
        if (diaSemana is null || diaSemana is < 0 or > 6)
            return Result<HorarioOperacionResponse>.BadRequest("El día debe estar entre 0 (domingo) y 6 (sábado).");

        if (!horaApertura.HasValue || !horaCierre.HasValue)
            return Result<HorarioOperacionResponse>.BadRequest("Las horas de apertura y cierre son obligatorias.");

        if (horaCierre <= horaApertura)
            return Result<HorarioOperacionResponse>.BadRequest("La hora de cierre debe ser posterior a la hora de apertura.");

        var dia = (byte)diaSemana.Value;
        if (await cierresFijosService.EsCierreFijoAsync(dia))
        {
            return Result<HorarioOperacionResponse>.Conflict(
                "Este día está configurado como cierre fijo. Elimine el cierre antes de configurar un horario.");
        }

        var horario = await horariosOperacionRepository.ObtenerPorDiaAsync(dia);
        var esNuevo = horario is null;
        if (esNuevo)
        {
            horario = new HorarioOperacion
            {
                DiaSemana = dia,
                HoraApertura = horaApertura.Value,
                HoraCierre = horaCierre.Value,
                Activo = true
            };
            horariosOperacionRepository.Agregar(horario);
            historialRepository.Agregar(CrearHistorialCreado(horario, idUsuario, Guid.NewGuid()));
        }
        else
        {
            if (horario!.HoraApertura == horaApertura.Value
                && horario.HoraCierre == horaCierre.Value
                && horario.Activo)
            {
                return Result<HorarioOperacionResponse>.Ok(Mapear(horario), "No hay cambios que guardar.");
            }

            var horaAperturaAnterior = horario.HoraApertura;
            var horaCierreAnterior = horario.HoraCierre;
            var activoAnterior = horario.Activo;

            horario.HoraApertura = horaApertura.Value;
            horario.HoraCierre = horaCierre.Value;
            horario.Activo = true;
            historialRepository.Agregar(CrearHistorialModificado(
                horario, idUsuario, Guid.NewGuid(), horaAperturaAnterior, horaCierreAnterior, activoAnterior));
        }

        try
        {
            var diasCerrados = await horariosOperacionRepository.ValidarCierresYGuardarCambiosAsync([dia]);
            if (diasCerrados.Count > 0)
            {
                return Result<HorarioOperacionResponse>.Conflict(
                    "Este día pasó a estar configurado como cierre fijo. No se guardó ningún cambio.");
            }
        }
        catch
        {
            return Result<HorarioOperacionResponse>.InternalError(
                "No se pudo guardar el horario. Intenta de nuevo.");
        }

        return Result<HorarioOperacionResponse>.Ok(
            Mapear(horario!),
            esNuevo ? "El horario de operación fue configurado correctamente."
                : "El horario de operación fue actualizado correctamente.");
    }

    public async Task<Result<List<HorarioOperacionResponse>>> GuardarLoteAsync(
        IEnumerable<GuardarHorarioOperacionLoteItemRequest>? horarios, int idUsuario)
    {
        var solicitudes = horarios?.ToList();
        if (solicitudes is null || solicitudes.Count == 0)
        {
            return Result<List<HorarioOperacionResponse>>.BadRequest(
                "Debes indicar al menos un horario para guardar.");
        }

        if (solicitudes.Any(h => h.DiaSemana is null || h.DiaSemana is < 0 or > 6))
        {
            return Result<List<HorarioOperacionResponse>>.BadRequest(
                "Los dias deben estar entre 0 (domingo) y 6 (sabado).");
        }

        if (solicitudes.GroupBy(h => h.DiaSemana!.Value).Any(g => g.Count() > 1))
        {
            return Result<List<HorarioOperacionResponse>>.BadRequest(
                "No se puede enviar mas de un horario para el mismo dia.");
        }

        if (solicitudes.Any(h => !h.HoraApertura.HasValue || !h.HoraCierre.HasValue))
        {
            return Result<List<HorarioOperacionResponse>>.BadRequest(
                "Las horas de apertura y cierre son obligatorias.");
        }

        if (solicitudes.Any(h => h.HoraCierre <= h.HoraApertura))
        {
            return Result<List<HorarioOperacionResponse>>.BadRequest(
                "La hora de cierre debe ser posterior a la hora de apertura.");
        }

        var dias = solicitudes.Select(h => (byte)h.DiaSemana!.Value).ToList();
        foreach (var dia in dias)
        {
            if (await cierresFijosService.EsCierreFijoAsync(dia))
            {
                return Result<List<HorarioOperacionResponse>>.Conflict(
                    "Este dia esta configurado como cierre fijo. Elimine el cierre antes de configurar un horario.");
            }
        }

        var existentes = await horariosOperacionRepository.ObtenerPorDiasAsync(dias);
        var existentesPorDia = existentes
            .GroupBy(h => h.DiaSemana)
            .ToDictionary(g => g.Key, g => g.First());
        var modificados = new List<HorarioOperacion>();
        var idOperacion = Guid.NewGuid();

        foreach (var solicitud in solicitudes)
        {
            var dia = (byte)solicitud.DiaSemana!.Value;
            var apertura = solicitud.HoraApertura!.Value;
            var cierre = solicitud.HoraCierre!.Value;
            if (!existentesPorDia.TryGetValue(dia, out var horario))
            {
                horario = new HorarioOperacion
                {
                    DiaSemana = dia,
                    HoraApertura = apertura,
                    HoraCierre = cierre,
                    Activo = true
                };
                horariosOperacionRepository.Agregar(horario);
                historialRepository.Agregar(CrearHistorialCreado(horario, idUsuario, idOperacion));
                modificados.Add(horario);
                continue;
            }

            if (horario.HoraApertura == apertura
                && horario.HoraCierre == cierre
                && horario.Activo)
            {
                continue;
            }

            var horaAperturaAnterior = horario.HoraApertura;
            var horaCierreAnterior = horario.HoraCierre;
            var activoAnterior = horario.Activo;

            horario.HoraApertura = apertura;
            horario.HoraCierre = cierre;
            horario.Activo = true;
            historialRepository.Agregar(CrearHistorialModificado(
                horario, idUsuario, idOperacion, horaAperturaAnterior, horaCierreAnterior, activoAnterior));
            modificados.Add(horario);
        }

        if (modificados.Count == 0)
            return Result<List<HorarioOperacionResponse>>.Ok([], "No habia cambios que guardar.");

        try
        {
            var diasCerrados = await horariosOperacionRepository.ValidarCierresYGuardarCambiosAsync(dias);
            if (diasCerrados.Count > 0)
            {
                return Result<List<HorarioOperacionResponse>>.Conflict(
                    "Uno o mas dias pasaron a estar configurados como cierre fijo. No se guardo ningun cambio.");
            }
        }
        catch
        {
            return Result<List<HorarioOperacionResponse>>.InternalError(
                "No se pudieron guardar los horarios. Intenta de nuevo.");
        }

        return Result<List<HorarioOperacionResponse>>.Ok(
            modificados.Select(Mapear).ToList(),
            "Los horarios de operacion fueron guardados correctamente.");
    }

    private static HistorialHorarioOperacion CrearHistorialCreado(
        HorarioOperacion horario, int idUsuario, Guid idOperacion) => new()
    {
        IdOperacion = idOperacion,
        DiaSemana = horario.DiaSemana,
        Accion = HistorialHorarioOperacionAcciones.Creado,
        HoraAperturaNueva = horario.HoraApertura,
        HoraCierreNuevo = horario.HoraCierre,
        ActivoNuevo = horario.Activo,
        IdUsuario = idUsuario,
        Fecha = DateTime.Now,
        // La relacion, no un IdHorarioOperacion provisional, permite a EF usar el IDENTITY real.
        HorarioOperacion = horario
    };

    private static HistorialHorarioOperacion CrearHistorialModificado(
        HorarioOperacion horario, int idUsuario, Guid idOperacion,
        TimeOnly horaAperturaAnterior, TimeOnly horaCierreAnterior, bool activoAnterior) => new()
    {
        IdOperacion = idOperacion,
        DiaSemana = horario.DiaSemana,
        Accion = HistorialHorarioOperacionAcciones.Modificado,
        HoraAperturaAnterior = horaAperturaAnterior,
        HoraAperturaNueva = horario.HoraApertura,
        HoraCierreAnterior = horaCierreAnterior,
        HoraCierreNuevo = horario.HoraCierre,
        ActivoAnterior = activoAnterior,
        ActivoNuevo = horario.Activo,
        IdUsuario = idUsuario,
        Fecha = DateTime.Now,
        HorarioOperacion = horario
    };

    private static HorarioOperacionResponse Mapear(HorarioOperacion horario) => new()
    {
        DiaSemana = horario.DiaSemana,
        HoraApertura = horario.HoraApertura,
        HoraCierre = horario.HoraCierre,
        Activo = horario.Activo
    };
}
