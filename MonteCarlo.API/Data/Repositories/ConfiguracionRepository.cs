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

    public void Agregar(ConfiguracionSistema configuracion) =>
        context.ConfiguracionesSistema.Add(configuracion);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}
