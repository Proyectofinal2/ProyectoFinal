using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class ConfiguracionRepository(MonteCarloDbContext context) : IConfiguracionRepository
{
    public Task<ConfiguracionSistema?> ObtenerAsync(string clave) =>
        context.ConfiguracionesSistema
            .Include(c => c.UsuarioModificacion)
            .FirstOrDefaultAsync(c => c.Clave == clave);

    public Task<List<ConfiguracionSistema>> ObtenerVariasAsync(IReadOnlyCollection<string> claves) =>
        context.ConfiguracionesSistema
            .Include(c => c.UsuarioModificacion)
            .Where(c => claves.Contains(c.Clave))
            .ToListAsync();

    public Task<List<ConfiguracionSistema>> ObtenerTodasAsync() =>
        context.ConfiguracionesSistema
            .Include(c => c.UsuarioModificacion)
            .OrderBy(c => c.Clave)
            .ToListAsync();

    public void Agregar(ConfiguracionSistema configuracion) =>
        context.ConfiguracionesSistema.Add(configuracion);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
