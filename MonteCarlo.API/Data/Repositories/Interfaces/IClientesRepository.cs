using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IClientesRepository
{
    /// <summary>
    /// Busca un cliente por teléfono para reutilizarlo en reservas
    /// subsecuentes del mismo número (HU-RES-003).
    /// </summary>
    Task<Cliente?> ObtenerPorTelefonoAsync(string telefono);

    void Agregar(Cliente cliente);

    Task GuardarCambiosAsync();
}