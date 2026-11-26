using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services;
using NSubstitute;

namespace MonteCarlo.Tests.Configuracion;

public class TasaIvaTests
{
    private readonly IConfiguracionRepository repositorio = Substitute.For<IConfiguracionRepository>();
    private ConfiguracionService CrearServicio() => new(repositorio, Substitute.For<IMetodosPagoRepository>(), Substitute.For<IHistorialMetodoPagoRepository>());

    [Theory]
    [InlineData("0", true)]
    [InlineData("13,25", true)]
    [InlineData("100.00", true)]
    [InlineData("100.01", false)]
    [InlineData("13.001", false)]
    [InlineData("-1", false)]
    [InlineData("abc", false)]
    public async Task ValidarLecturaYActualizacion(string valor, bool valido)
    {
        var config = new ConfiguracionSistema { Clave = ConfiguracionService.ClaveTasaIva, Valor = valor };
        repositorio.ObtenerAsync(config.Clave).Returns(config);
        repositorio.ObtenerVariasAsync(Arg.Any<IReadOnlyCollection<string>>()).Returns(new List<ConfiguracionSistema> { config });
        repositorio.ObtenerTodasAsync().Returns(new List<ConfiguracionSistema> { config });
        var servicio = CrearServicio();
        Assert.Equal(valido, (await servicio.ObtenerTasaIvaAsync()).Success);
        var resultado = await servicio.ActualizarConfiguracionGeneralAsync(new ActualizarConfiguracionGeneralRequest
        {
            Parametros = [new ActualizarParametroItem { Clave = config.Clave, Valor = valor }]
        }, 7);
        Assert.Equal(valido, resultado.Success);
    }

    [Fact]
    public async Task AusenteDevuelveErrorExplicito()
    {
        var resultado = await CrearServicio().ObtenerTasaIvaAsync();
        Assert.False(resultado.Success);
        Assert.Contains("configurada", resultado.Message);
    }

    [Fact]
    public async Task CambioRegistraUsuarioYFecha()
    {
        var config = new ConfiguracionSistema { Clave = ConfiguracionService.ClaveTasaIva, Valor = "13" };
        repositorio.ObtenerVariasAsync(Arg.Any<IReadOnlyCollection<string>>()).Returns(new List<ConfiguracionSistema> { config });
        repositorio.ObtenerTodasAsync().Returns(new List<ConfiguracionSistema> { config });
        var resultado = await CrearServicio().ActualizarConfiguracionGeneralAsync(new ActualizarConfiguracionGeneralRequest
        {
            Parametros = [new ActualizarParametroItem { Clave = config.Clave, Valor = "15.25" }]
        }, 7);
        Assert.True(resultado.Success);
        Assert.Equal(7, config.IdUsuarioModificacion);
        Assert.True(config.FechaModificacion > DateTime.Now.AddMinutes(-1));
        await repositorio.Received(1).GuardarCambiosAsync();
    }
}
