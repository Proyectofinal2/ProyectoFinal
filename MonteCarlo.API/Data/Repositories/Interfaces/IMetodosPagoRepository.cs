using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IMetodosPagoRepository
{
    /// <summary>Metodos de pago activos, en orden, para ofrecer al cobrar.</summary>
    Task<List<MetodoPago>> ObtenerActivosAsync();

    Task<MetodoPago?> ObtenerPorIdAsync(int idMetodoPago);
}
