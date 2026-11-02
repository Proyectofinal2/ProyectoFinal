using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IHistorialHorarioOperacionService
{
    const int TamanoPagina = 10;

    Task<Result<PaginaResponse<HistorialHorarioOperacionResponse>>> ObtenerAsync(
        string? busqueda, int? diaSemana, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina, int tamano);

    Task<Result<List<UsuarioHistorialResponse>>> ObtenerUsuariosAsync();
}
