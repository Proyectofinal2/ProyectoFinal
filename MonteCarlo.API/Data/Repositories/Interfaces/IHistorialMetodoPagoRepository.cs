using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IHistorialMetodoPagoRepository
{
    /// <summary>Solo agrega al contexto; se persiste junto con el método que lo origina.</summary>
    void Agregar(HistorialMetodoPago entrada);

    Task<(List<HistorialMetodoPago> Items, int Total)> BuscarAsync(
        string? busqueda, int? idMetodoPago, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina, int tamano);

    Task<List<Usuario>> ObtenerUsuariosAsync();
}
