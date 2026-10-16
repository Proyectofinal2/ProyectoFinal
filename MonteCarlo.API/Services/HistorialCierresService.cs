using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HistorialCierresService(IHistorialCierresRepository historialCierresRepository) : IHistorialCierresService
{
    public async Task<Result<List<HistorialCierreResponse>>> ObtenerAsync(
        string? tipo, string? accion, DateOnly? desde, DateOnly? hasta)
    {
        if (tipo is not null && tipo is not ("Fijo" or "Eventual"))
            return Result<List<HistorialCierreResponse>>.BadRequest("El tipo de cierre debe ser \"Fijo\" o \"Eventual\".");

        if (accion is not null && accion is not ("Registrado" or "Eliminado"))
            return Result<List<HistorialCierreResponse>>.BadRequest("La accion debe ser \"Registrado\" o \"Eliminado\".");

        if (desde > hasta)
            return Result<List<HistorialCierreResponse>>.BadRequest("La fecha inicial no puede ser posterior a la final.");

        var historial = await historialCierresRepository.BuscarAsync(tipo, accion, desde, hasta);

        var respuesta = historial.Select(h => new HistorialCierreResponse
        {
            IdHistorialCierre = h.IdHistorialCierre,
            Fecha = h.Fecha,
            TipoCierre = h.TipoCierre,
            Accion = h.Accion,
            DiaSemana = h.DiaSemana,
            FechaCierre = h.FechaCierre,
            Motivo = h.Motivo,
            Usuario = h.Usuario.NombreCompleto
        }).ToList();

        return Result<List<HistorialCierreResponse>>.Ok(respuesta, "Historial obtenido exitosamente.");
    }
}
