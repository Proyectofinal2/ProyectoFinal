namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Auditoria de cambios en productos y categorias (HU-CON-001). Una fila por campo
/// cambiado. Una sola tabla para paginar y filtrar el historial general sin UNION.
/// </summary>
public class HistorialCatalogo
{
    public int IdHistorialCatalogo { get; set; }

    /// <summary>Producto / Categoria (ver <see cref="HistorialCatalogoTipos"/>).</summary>
    public string TipoElemento { get; set; } = null!;

    /// <summary>Solo si TipoElemento es Producto.</summary>
    public int? IdProducto { get; set; }

    /// <summary>Solo si TipoElemento es Categoria.</summary>
    public int? IdCategoria { get; set; }

    /// <summary>
    /// Nombre del producto o la categoria al momento de eliminarlo. Solo se llena en el historial de un elemento
    /// eliminado (IdProducto / IdCategoria pasan a NULL); mientras exista, el nombre sale de la relacion.
    /// </summary>
    public string? NombreElemento { get; set; }

    /// <summary>Creado / Editado / Reordenado / Habilitado / Deshabilitado / Eliminado.</summary>
    public string Accion { get; set; } = null!;

    /// <summary>Obligatorio en Editado y Reordenado (ver <see cref="HistorialCatalogoCampos"/>).</summary>
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }

    /// <summary>Explica los cambios en cascada (ej. "Por deshabilitar la categoria «Bebidas»").</summary>
    public string? Motivo { get; set; }

    /// <summary>Administrador responsable.</summary>
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }
    public CategoriaProducto? Categoria { get; set; }
    public Usuario Usuario { get; set; } = null!;
}

public static class HistorialCatalogoTipos
{
    public const string Producto = "Producto";
    public const string Categoria = "Categoria";
}

public static class HistorialCatalogoAcciones
{
    public const string Creado = "Creado";
    public const string Editado = "Editado";
    public const string Reordenado = "Reordenado";
    public const string Habilitado = "Habilitado";
    public const string Deshabilitado = "Deshabilitado";
    public const string Eliminado = "Eliminado";

    public static readonly string[] Todas = [Creado, Editado, Reordenado, Habilitado, Deshabilitado, Eliminado];
}

/// <summary>
/// Prefijos del motivo de las filas de producto generadas en cascada al habilitar o deshabilitar
/// una categoria. El historial general las omite: la fila de la categoria ya resume cuantos
/// productos cambiaron. Siguen en el historial de cada producto.
/// </summary>
public static class HistorialCatalogoMotivos
{
    public const string PorDeshabilitarCategoria = "Por deshabilitar la categoría «";
    public const string PorHabilitarCategoria = "Por habilitar la categoría «";
}

public static class HistorialCatalogoCampos
{
    public const string Nombre = "Nombre";
    public const string Precio = "Precio";
    public const string Categoria = "Categoria";
    public const string Posicion = "Posicion";
}
