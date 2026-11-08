using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IHistorialMetodoPagoService
{
    const int TamanoPagina = 10;

    Task<Result<PaginaResponse<HistorialMetodoPagoResponse>>> ObtenerAsync(
        string? busqueda, int? idMetodoPago, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina, int tamano);

    Task<Result<List<UsuarioHistorialResponse>>> ObtenerUsuariosAsync();
}
