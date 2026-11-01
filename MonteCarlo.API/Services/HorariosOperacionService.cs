using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HorariosOperacionService(
    IHorariosOperacionRepository horariosOperacionRepository,
    ICierresFijosService cierresFijosService) : IHorariosOperacionService
{
    public async Task<Result<List<HorarioOperacionResponse>>> ObtenerAsync()
    {
        var horarios = await horariosOperacionRepository.ObtenerTodosAsync();
        var respuesta = horarios.GroupBy(h => h.DiaSemana).Select(g => Mapear(g.First())).ToList();
        return Result<List<HorarioOperacionResponse>>.Ok(respuesta, "Horarios de operación obtenidos exitosamente.");
    }

    public async Task<Result<HorarioOperacionResponse>> GuardarAsync(int? diaSemana, TimeOnly? horaApertura, TimeOnly? horaCierre)
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
        if (horario is null)
        {
            horario = new HorarioOperacion
            {
                DiaSemana = dia,
                HoraApertura = horaApertura.Value,
                HoraCierre = horaCierre.Value,
                Activo = true
            };
            horariosOperacionRepository.Agregar(horario);
            await horariosOperacionRepository.GuardarCambiosAsync();

            return Result<HorarioOperacionResponse>.Ok(Mapear(horario),
                "El horario de operación fue configurado correctamente.");
        }

        horario.HoraApertura = horaApertura.Value;
        horario.HoraCierre = horaCierre.Value;
        horario.Activo = true;
        await horariosOperacionRepository.GuardarCambiosAsync();

        return Result<HorarioOperacionResponse>.Ok(Mapear(horario), "El horario de operación fue actualizado correctamente.");
    }

    public async Task<Result<List<HorarioOperacionResponse>>> GuardarLoteAsync(
        IEnumerable<GuardarHorarioOperacionLoteItemRequest>? horarios)
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

        foreach (var solicitud in solicitudes)
        {
            var dia = (byte)solicitud.DiaSemana!.Value;
            var horaApertura = solicitud.HoraApertura!.Value;
            var horaCierre = solicitud.HoraCierre!.Value;
            if (!existentesPorDia.TryGetValue(dia, out var horario))
            {
                horario = new HorarioOperacion
                {
                    DiaSemana = dia,
                    HoraApertura = horaApertura,
                    HoraCierre = horaCierre,
                    Activo = true
                };
                horariosOperacionRepository.Agregar(horario);
                modificados.Add(horario);
                continue;
            }

            if (horario.HoraApertura == horaApertura
                && horario.HoraCierre == horaCierre
                && horario.Activo)
            {
                continue;
            }

            horario.HoraApertura = horaApertura;
            horario.HoraCierre = horaCierre;
            horario.Activo = true;
            modificados.Add(horario);
        }

        if (modificados.Count == 0)
        {
            return Result<List<HorarioOperacionResponse>>.Ok([], "No habia cambios que guardar.");
        }

        try
        {
            var diasCerrados = await horariosOperacionRepository.ValidarCierresYGuardarCambiosLoteAsync(dias);
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

    private static HorarioOperacionResponse Mapear(HorarioOperacion horario) => new()
    {
        DiaSemana = horario.DiaSemana,
        HoraApertura = horario.HoraApertura,
        HoraCierre = horario.HoraCierre,
        Activo = horario.Activo
    };
}
