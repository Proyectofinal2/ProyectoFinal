using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services;

/// <summary>Adyacencia entre mesas por distancia euclidiana (PosX/PosY) contra un radio fijo; nunca se persiste.</summary>
public static class MesaGeometryHelper
{
    public const double RadioAdyacenciaPx = 200;

    public static bool SonAdyacentes(Mesa a, Mesa b)
    {
        var deltaX = a.PosX - b.PosX;
        var deltaY = a.PosY - b.PosY;
        var distancia = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        return distancia <= RadioAdyacenciaPx;
    }
}
