using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

/// <summary>Historial de cambios de cuentas de administrador (HU-AUT-007).</summary>
public interface IHistorialUsuarioService
{
    const int TamanoPagina = 10;

    Task<Result<PaginaResponse<HistorialUsuarioResponse>>> ObtenerAsync(
        string? busqueda, int? idUsuarioAfectado, int? idUsuario, string? accion, int pagina, int tamano);

    Task<Result<List<UsuarioHistorialResponse>>> ObtenerResponsablesAsync();
}
