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

    private static HorarioOperacionResponse Mapear(HorarioOperacion horario) => new()
    {
        DiaSemana = horario.DiaSemana,
        HoraApertura = horario.HoraApertura,
        HoraCierre = horario.HoraCierre,
        Activo = horario.Activo
    };
}
