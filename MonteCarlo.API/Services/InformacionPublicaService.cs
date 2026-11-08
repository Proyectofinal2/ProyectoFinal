using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>Compone la información mínima de horarios para visitantes, sin exponer datos administrativos.</summary>
public class InformacionPublicaService(
    IHorariosOperacionService horariosOperacionService,
    ICierresFijosService cierresFijosService) : IInformacionPublicaService
{
    private const string Abierto = "Abierto";
    private const string Cerrado = "Cerrado";
    private const string SinHorario = "SinHorario";

    // El orden de presentación público empieza en lunes; los valores preservan
    // la convención de la base de datos (0=domingo ... 6=sábado).
    private static readonly int[] DiasSemana = [1, 2, 3, 4, 5, 6, 0];

    public async Task<Result<HorariosPublicosResponse>> ObtenerHorariosAsync()
    {
        var horariosResult = await horariosOperacionService.ObtenerAsync();
        var cierresResult = await cierresFijosService.ObtenerAsync();
        if (!horariosResult.Success || !cierresResult.Success)
        {
            return Result<HorariosPublicosResponse>.InternalError(
                "No se pudieron obtener los horarios de atención.");
        }

        var horariosPorDia = (horariosResult.Data ?? [])
            .GroupBy(h => h.DiaSemana)
            .ToDictionary(g => g.Key, g => g.First());
        var diasCerrados = (cierresResult.Data?.Dias ?? []).ToHashSet();

        var respuesta = new HorariosPublicosResponse
        {
            Dias = DiasSemana.Select(dia => CrearDia(dia, horariosPorDia, diasCerrados)).ToList()
        };

        return Result<HorariosPublicosResponse>.Ok(respuesta,
            "Horarios de atención obtenidos exitosamente.");
    }

    private static HorarioPublicoDiaResponse CrearDia(
        int diaSemana,
        IReadOnlyDictionary<int, HorarioOperacionResponse> horariosPorDia,
        IReadOnlySet<int> diasCerrados)
    {
        // El cierre fijo tiene prioridad sobre cualquier horario persistido.
        if (diasCerrados.Contains(diaSemana))
        {
            return new HorarioPublicoDiaResponse { DiaSemana = diaSemana, Estado = Cerrado };
        }

        if (!horariosPorDia.TryGetValue(diaSemana, out var horario))
        {
            return new HorarioPublicoDiaResponse { DiaSemana = diaSemana, Estado = SinHorario };
        }

        if (!horario.Activo)
        {
            return new HorarioPublicoDiaResponse { DiaSemana = diaSemana, Estado = Cerrado };
        }

        return new HorarioPublicoDiaResponse
        {
            DiaSemana = diaSemana,
            Estado = Abierto,
            HoraApertura = horario.HoraApertura,
            HoraCierre = horario.HoraCierre
        };
    }
}
