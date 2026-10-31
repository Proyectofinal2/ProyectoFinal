using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services;

/// <summary>
/// Arma las entradas de HistorialCatalogo. Recibe las entidades (no los ids) para que
/// tambien sirva con filas nuevas cuyo id aun no existe: EF resuelve la FK al guardar.
/// </summary>
internal static class HistorialCatalogoFactory
{
    public static HistorialCatalogo DeProducto(
        Producto producto, int idUsuario, string accion,
        string? campo = null, string? anterior = null, string? nuevo = null, string? motivo = null) =>
        new()
        {
            TipoElemento = HistorialCatalogoTipos.Producto,
            Producto = producto,
            IdUsuario = idUsuario,
            Fecha = DateTime.Now,
            Accion = accion,
            Campo = campo,
            ValorAnterior = anterior,
            ValorNuevo = nuevo,
            Motivo = motivo
        };

    public static HistorialCatalogo DeCategoria(
        CategoriaProducto categoria, int idUsuario, string accion,
        string? campo = null, string? anterior = null, string? nuevo = null, string? motivo = null) =>
        new()
        {
            TipoElemento = HistorialCatalogoTipos.Categoria,
            Categoria = categoria,
            IdUsuario = idUsuario,
            Fecha = DateTime.Now,
            Accion = accion,
            Campo = campo,
            ValorAnterior = anterior,
            ValorNuevo = nuevo,
            Motivo = motivo
        };
}
