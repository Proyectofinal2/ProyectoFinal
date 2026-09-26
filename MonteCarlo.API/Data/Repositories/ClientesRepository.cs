using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class ClientesRepository(MonteCarloDbContext context) : IClientesRepository
{
    public Task<Cliente?> ObtenerPorTelefonoAsync(string telefono) =>
        context.Clientes
            .FirstOrDefaultAsync(c => c.Telefono == telefono);

    public void Agregar(Cliente cliente) => context.Clientes.Add(cliente);

    public Task GuardarCambiosAsync() => context.SaveChangesAsync();
}