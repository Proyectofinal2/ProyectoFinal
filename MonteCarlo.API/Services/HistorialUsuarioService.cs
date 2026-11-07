using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HistorialUsuarioService(IHistorialUsuarioRepository historialRepository) : IHistorialUsuarioService
{
    public async Task<Result<PaginaResponse<HistorialUsuarioResponse>>> ObtenerAsync(
        string? busqueda, int? idUsuarioAfectado, int? idUsuario, string? accion, int pagina, int tamano)
    {
        if (accion is not null && !HistorialUsuarioAcciones.Todas.Contains(accion))
            return Result<PaginaResponse<HistorialUsuarioResponse>>.BadRequest(
                $"La acción debe ser una de: {string.Join(", ", HistorialUsuarioAcciones.Todas)}.");

        if (pagina < 1)
            return Result<PaginaResponse<HistorialUsuarioResponse>>.BadRequest("La página debe ser 1 o mayor.");

        tamano = Math.Clamp(tamano, 1, IHistorialUsuarioService.TamanoPagina);

        var (items, total) = await historialRepository.BuscarAsync(busqueda, idUsuarioAfectado, idUsuario, accion, pagina, tamano);

        return Result<PaginaResponse<HistorialUsuarioResponse>>.Ok(new PaginaResponse<HistorialUsuarioResponse>
        {
            Items = items.Select(Mapear).ToList(),
            Pagina = pagina,
            TamanoPagina = tamano,
            Total = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)tamano)
        }, "Historial obtenido exitosamente.");
    }

    public async Task<Result<List<UsuarioHistorialResponse>>> ObtenerResponsablesAsync()
    {
        var usuarios = await historialRepository.ObtenerResponsablesAsync();
        return Result<List<UsuarioHistorialResponse>>.Ok(
            usuarios.Select(u => new UsuarioHistorialResponse { IdUsuario = u.IdUsuario, NombreCompleto = u.NombreCompleto }).ToList(),
            "Usuarios obtenidos exitosamente.");
    }

    private static HistorialUsuarioResponse Mapear(HistorialUsuario h) => new()
    {
        IdHistorialUsuario = h.IdHistorialUsuario,
        Fecha = h.Fecha,
        IdUsuario = h.IdUsuario,
        Usuario = h.Usuario.NombreCompleto,
        RolUsuario = h.Usuario.Rol,
        IdUsuarioAfectado = h.IdUsuarioAfectado,
        NombreUsuarioAfectado = h.NombreUsuarioAfectado,
        Accion = h.Accion,
        Campo = h.Campo,
        ValorAnterior = h.ValorAnterior,
        ValorNuevo = h.ValorNuevo,
        Motivo = h.Motivo
    };
}
