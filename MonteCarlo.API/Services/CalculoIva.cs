namespace MonteCarlo.API.Services;

/// <summary>Calcula el impuesto una sola vez sobre el subtotal de la venta.</summary>
public static class CalculoIva
{
    public static (decimal ImporteIva, decimal Total) Calcular(decimal subtotal, decimal tasaPorcentaje)
    {
        var impuesto = decimal.Round(subtotal * tasaPorcentaje / 100m, 2, MidpointRounding.AwayFromZero);
        return (impuesto, subtotal + impuesto);
    }
}
