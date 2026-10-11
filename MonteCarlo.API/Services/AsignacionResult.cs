using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services;

/// <summary>
/// Resultado de <see cref="ReservasService"/>.BuscarMejorAsignacion: 1 mesa
/// individual, 2 mesas adyacentes combinadas, o ninguna (RequiereManual).
/// Usado por la creacion automatica de reservas, la sugerencia al admin
/// y el calculo de disponibilidad del calendario.
/// </summary>
public sealed record AsignacionResult(List<Mesa> Mesas, bool RequiereManual)
{
    public bool EsCombinacionDeMesas => Mesas.Count > 1;
}
