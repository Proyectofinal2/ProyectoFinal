namespace MonteCarlo.API.Services;

/// <summary>
/// Cuando una cuenta abierta pasa a ser "antigua" o por cobrar (HU-CON-002): cuando termino el tiempo de uso de la
/// mesa (llegada + duracion del turno) y se agoto ademas un margen. Es la misma referencia que la liberacion estimada
/// de la mesa (ver ReservasService.LiberacionEstimada), con una tolerancia extra para cobrar.
/// Funcion pura: no consulta la base.
/// </summary>
public static class CuentaVencimiento
{
    /// <summary>Fecha y hora desde la cual la cuenta es antigua: llegada + duracion del turno + margen.</summary>
    /// <param name="ancla">Cuando se sento el cliente (o abrio la cuenta si no hay llegada registrada).</param>
    public static DateTime Calcular(DateTime ancla, int duracionMinutos, int toleranciaMinutos) =>
        ancla.AddMinutes(duracionMinutos + toleranciaMinutos);

    /// <summary>true si <paramref name="ahora"/> ya paso el vencimiento de la cuenta.</summary>
    public static bool EstaVencida(DateTime ancla, int duracionMinutos, int toleranciaMinutos, DateTime ahora) =>
        ahora > Calcular(ancla, duracionMinutos, toleranciaMinutos);
}
