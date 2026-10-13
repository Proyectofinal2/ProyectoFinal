using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services;

/// <summary>Resultado de BuscarMejorAsignacion: 1 mesa, 2 combinadas, o ninguna (RequiereManual).</summary>
public sealed record AsignacionResult(List<Mesa> Mesas, bool RequiereManual)
{
    public bool EsCombinacionDeMesas => Mesas.Count > 1;
}
