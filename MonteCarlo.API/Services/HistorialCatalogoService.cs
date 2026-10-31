using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HistorialCatalogoService(
    IHistorialCatalogoRepository historialRepository,
    IProductosRepository productosRepository) : IHistorialCatalogoService
{
    private const int MaximoPorProducto = 50;

    public async Task<Result<PaginaResponse<HistorialCatalogoResponse>>> ObtenerAsync(
        string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina, int tamano)
    {
        if (tipo is not null && tipo is not (HistorialCatalogoTipos.Producto or HistorialCatalogoTipos.Categoria))
            return Result<PaginaResponse<HistorialCatalogoResponse>>.BadRequest("El tipo debe ser \"Producto\" o \"Categoria\".");

        if (accion is not null && !HistorialCatalogoAcciones.Todas.Contains(accion))
            return Result<PaginaResponse<HistorialCatalogoResponse>>.BadRequest(
                $"La acción debe ser una de: {string.Join(", ", HistorialCatalogoAcciones.Todas)}.");

        if (pagina < 1)
            return Result<PaginaResponse<HistorialCatalogoResponse>>.BadRequest("La página debe ser 1 o mayor.");

        tamano = Math.Clamp(tamano, 1, IHistorialCatalogoService.TamanoPagina);

        var (items, total) = await historialRepository.BuscarAsync(busqueda, tipo, idUsuario, accion, pagina, tamano);

        return Result<PaginaResponse<HistorialCatalogoResponse>>.Ok(new PaginaResponse<HistorialCatalogoResponse>
        {
            Items = items.Select(Mapear).ToList(),
            Pagina = pagina,
            TamanoPagina = tamano,
            Total = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)tamano)
        }, "Historial obtenido exitosamente.");
    }

    public async Task<Result<List<HistorialCatalogoResponse>>> ObtenerPorProductoAsync(int idProducto)
    {
        if (await productosRepository.ObtenerPorIdAsync(idProducto) is null)
            return Result<List<HistorialCatalogoResponse>>.NotFound("El producto no existe.");

        var items = await historialRepository.ObtenerPorProductoAsync(idProducto, MaximoPorProducto);
        return Result<List<HistorialCatalogoResponse>>.Ok(items.Select(Mapear).ToList(), "Historial obtenido exitosamente.");
    }

    public async Task<Result<List<UsuarioHistorialResponse>>> ObtenerUsuariosAsync()
    {
        var usuarios = await historialRepository.ObtenerUsuariosAsync();
        return Result<List<UsuarioHistorialResponse>>.Ok(
            usuarios.Select(u => new UsuarioHistorialResponse { IdUsuario = u.IdUsuario, NombreCompleto = u.NombreCompleto }).ToList(),
            "Usuarios obtenidos exitosamente.");
    }

    // La restriccion CK_HistorialCatalogo_Elemento garantiza que exista el producto o la categoria segun el tipo.
    private static HistorialCatalogoResponse Mapear(HistorialCatalogo h)
    {
        var esProducto = h.TipoElemento == HistorialCatalogoTipos.Producto;
        var categoria = esProducto ? h.Producto?.Categoria : h.Categoria;

        // Elemento eliminado: su historial sigue ahi con el nombre guardado (IdProducto / IdCategoria son NULL).
        var nombreElemento = esProducto
            ? h.Producto?.Nombre ?? h.NombreElemento ?? "Producto eliminado"
            : categoria?.Nombre ?? h.NombreElemento ?? "Categoría eliminada";

        return new HistorialCatalogoResponse
        {
            IdHistorialCatalogo = h.IdHistorialCatalogo,
            Fecha = h.Fecha,
            Usuario = h.Usuario.NombreCompleto,
            RolUsuario = h.Usuario.Rol,
            TipoElemento = h.TipoElemento,
            IdElemento = (esProducto ? h.IdProducto : h.IdCategoria) ?? 0,
            NombreElemento = nombreElemento,
            IdCategoriaElemento = categoria?.IdCategoria ?? 0,
            CategoriaElemento = categoria?.Nombre ?? nombreElemento,
            Accion = h.Accion,
            Campo = h.Campo,
            ValorAnterior = h.ValorAnterior,
            ValorNuevo = h.ValorNuevo,
            Motivo = h.Motivo
        };
    }
}
