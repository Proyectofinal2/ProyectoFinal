using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HistorialMetodoPagoService(
    IHistorialMetodoPagoRepository historialRepository) : IHistorialMetodoPagoService
{
    public async Task<Result<PaginaResponse<HistorialMetodoPagoResponse>>> ObtenerAsync(
        string? busqueda, int? idMetodoPago, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina, int tamano)
    {
        if (idMetodoPago is < 1)
            return Result<PaginaResponse<HistorialMetodoPagoResponse>>.BadRequest("El método de pago debe ser válido.");

        if (accion is not null && !HistorialMetodoPagoAcciones.Todas.Contains(accion))
            return Result<PaginaResponse<HistorialMetodoPagoResponse>>.BadRequest(
                $"La acción debe ser una de: {string.Join(", ", HistorialMetodoPagoAcciones.Todas)}.");

        if (desde > hasta)
            return Result<PaginaResponse<HistorialMetodoPagoResponse>>.BadRequest(
                "La fecha inicial no puede ser posterior a la final.");

        if (pagina < 1)
            return Result<PaginaResponse<HistorialMetodoPagoResponse>>.BadRequest("La página debe ser 1 o mayor.");

        tamano = Math.Clamp(tamano, 1, IHistorialMetodoPagoService.TamanoPagina);
        var (items, total) = await historialRepository.BuscarAsync(
            busqueda?.Trim(), idMetodoPago, idUsuario, accion, desde, hasta, pagina, tamano);

        return Result<PaginaResponse<HistorialMetodoPagoResponse>>.Ok(
            new PaginaResponse<HistorialMetodoPagoResponse>
            {
                Items = items.Select(Mapear).ToList(),
                Pagina = pagina,
                TamanoPagina = tamano,
                Total = total,
                TotalPaginas = (int)Math.Ceiling(total / (double)tamano)
            },
            "Historial obtenido exitosamente.");
    }

    public async Task<Result<List<UsuarioHistorialResponse>>> ObtenerUsuariosAsync()
    {
        var usuarios = await historialRepository.ObtenerUsuariosAsync();
        return Result<List<UsuarioHistorialResponse>>.Ok(
            usuarios.Select(u => new UsuarioHistorialResponse
            {
                IdUsuario = u.IdUsuario,
                NombreCompleto = u.NombreCompleto
            }).ToList(),
            "Usuarios obtenidos exitosamente.");
    }

    private static HistorialMetodoPagoResponse Mapear(HistorialMetodoPago historial) => new()
    {
        IdHistorialMetodoPago = historial.IdHistorialMetodoPago,
        IdOperacion = historial.IdOperacion,
        IdMetodoPago = historial.IdMetodoPago,
        Accion = historial.Accion,
        NombreAnterior = historial.NombreAnterior,
        NombreNuevo = historial.NombreNuevo,
        ActivoAnterior = historial.ActivoAnterior,
        ActivoNuevo = historial.ActivoNuevo,
        IdUsuario = historial.IdUsuario,
        Usuario = historial.Usuario.NombreCompleto,
        RolUsuario = historial.Usuario.Rol,
        Fecha = historial.Fecha
    };
}
