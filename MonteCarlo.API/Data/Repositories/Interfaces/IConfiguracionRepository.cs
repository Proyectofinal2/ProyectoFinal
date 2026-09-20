using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IConfiguracionRepository
{
    /// Parámetro con el usuario que lo modificó (entidad rastreada), o null si no existe.
    Task<ConfiguracionSistema?> ObtenerAsync(string clave);

    void Agregar(ConfiguracionSistema configuracion);

    Task GuardarCambiosAsync();
}
