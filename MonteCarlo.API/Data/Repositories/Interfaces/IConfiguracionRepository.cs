using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IConfiguracionRepository
{
    /// Par�metro con el usuario que lo modific� (entidad rastreada), o null si no existe.
    Task<ConfiguracionSistema?> ObtenerAsync(string clave);

    /// Varios parametros en una sola consulta (evita 1 round-trip por clave).
    Task<List<ConfiguracionSistema>> ObtenerVariasAsync(IReadOnlyCollection<string> claves);

    /// Todos los parametros existentes, para la pantalla dinamica de Parametros
    /// (muestra lo que haya en la tabla, no un set fijo de claves).
    Task<List<ConfiguracionSistema>> ObtenerTodasAsync();

    void Agregar(ConfiguracionSistema configuracion);

    Task GuardarCambiosAsync();
}
