using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IMesasRepository
{
    /// <summary>Todas las mesas que no estan deshabilitadas por mantenimiento, sin filtrar por capacidad (para armar combinaciones).</summary>
    Task<List<Mesa>> ObtenerMesasHabilitadasAsync();

    /// <summary>
    /// Horas con una reserva activa (Pendiente, Confirmada) de todas las mesas en una fecha,
    /// en una sola consulta -- evita N+1 al calcular disponibilidad/combinaciones del dia completo.
    /// </summary>
    Task<Dictionary<int, List<TimeOnly>>> ObtenerHorasReservadasPorFechaAsync(DateOnly fecha);

    /// <summary>
    /// Igual que <see cref="ObtenerHorasReservadasPorFechaAsync"/> pero para un rango de fechas
    /// completo en una sola consulta (clave externa: fecha; clave interna: IdMesa) -- evita
    /// repetir una consulta por cada dia al calcular la disponibilidad de un mes completo.
    /// </summary>
    Task<Dictionary<DateOnly, Dictionary<int, List<TimeOnly>>>> ObtenerHorasReservadasPorRangoAsync(
        DateOnly inicio, DateOnly fin);

    /// <summary>Mesas por Id, para validar la asignación manual/reasignación elegida por el admin (HU-MES-005/006).</summary>
    Task<List<Mesa>> ObtenerPorIdsAsync(List<int> idsMesa);
}