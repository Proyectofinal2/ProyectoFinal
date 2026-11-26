using MonteCarlo.API.Services;

namespace MonteCarlo.Tests.Ventas;

public class CalculoIvaTests
{
    [Theory]
    [InlineData("10000", "13", "1300", "11300")]
    [InlineData("0.50", "1", "0.01", "0.51")]
    [InlineData("0.49", "1", "0.00", "0.49")]
    [InlineData("2500", "0", "0", "2500")]
    public void CalculaImpuestoSobreSubtotalConRedondeoComercial(
        string subtotal, string tasa, string impuesto, string total)
    {
        var cultura = System.Globalization.CultureInfo.InvariantCulture;
        var resultado = CalculoIva.Calcular(decimal.Parse(subtotal, cultura), decimal.Parse(tasa, cultura));
        Assert.Equal(decimal.Parse(impuesto, cultura), resultado.ImporteIva);
        Assert.Equal(decimal.Parse(total, cultura), resultado.Total);
    }
}
