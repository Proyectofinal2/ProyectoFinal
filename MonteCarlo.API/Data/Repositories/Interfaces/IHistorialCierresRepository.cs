using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IHistorialCierresRepository
{
    /// Ultimos cambios, del mas reciente al mas antiguo. Cada filtro es opcional; hasta incluye todo ese dia.
    Task<List<HistorialCierre>> BuscarAsync(string? tipo, string? accion, DateOnly? desde, DateOnly? hasta);
}
