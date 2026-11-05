using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IMetodosPagoRepository
{
    Task<List<MetodoPago>> ObtenerActivosAsync();
    Task<MetodoPago?> ObtenerPorIdAsync(int idMetodoPago);

    // Métodos nuevos para HU-CFG-007
    Task<List<MetodoPago>> ObtenerTodosAsync();
    Task<bool> ExisteNombreAsync(string nombre);
    void Agregar(MetodoPago metodo);
    Task GuardarCambiosAsync();
}