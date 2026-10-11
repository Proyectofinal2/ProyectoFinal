using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services;

/// <summary>
/// Calculo de adyacencia entre mesas a partir de su posicion en el canvas
/// (PosX/PosY). La adyacencia nunca se guarda como relacion explicita
/// mesa-mesa: se deriva por distancia euclidiana contra un radio fijo.
/// Compartido entre el algoritmo de combinacion (ReservasService) y, mas
/// adelante, el canvas de mesas (MesasService).
/// </summary>
public static class MesaGeometryHelper
{
    public const double RadioAdyacenciaPx = 150;

    public static bool SonAdyacentes(Mesa a, Mesa b)
    {
        var deltaX = a.PosX - b.PosX;
        var deltaY = a.PosY - b.PosY;
        var distancia = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        return distancia <= RadioAdyacenciaPx;
    }
}
