namespace MonteCarlo.WEB.Models.UI;

/// <summary>
/// Metodo de pago para mostrarlo con icono y color propio (_MetodoPago). <paramref name="Nombre"/> nulo = la venta no tiene
/// metodo de pago (cuenta abierta, abandonada o sin cobro). <paramref name="Activo"/> = false si el metodo se deshabilito
/// despues: la venta lo conserva, pero se ve atenuado y marcado.
/// </summary>
public record MetodoPagoVisualViewModel(string? Nombre, bool Activo = true)
{
    /// <summary>Clave de estilo (clase CSS) e icono de Bootstrap Icons segun el nombre del metodo.</summary>
    public (string Clave, string Icono) Estilo => Nombre?.Trim().ToLowerInvariant() switch
    {
        { } n when n.Contains("efectivo") => ("efectivo", "bi-cash-coin"),
        { } n when n.Contains("tarjeta") => ("tarjeta", "bi-credit-card"),
        { } n when n.Contains("sinpe") => ("sinpe", "bi-phone"),
        _ => ("otro", "bi-wallet2")
    };
}
