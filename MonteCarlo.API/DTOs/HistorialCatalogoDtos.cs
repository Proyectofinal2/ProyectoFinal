namespace MonteCarlo.API.DTOs;

/// <summary>Una fila del historial de cambios de productos y categorias (HU-CON-001).</summary>
public class HistorialCatalogoResponse
{
    public int IdHistorialCatalogo { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = null!;

    /// <summary>Rol del administrador que hizo el cambio: General / Regular.</summary>
    public string RolUsuario { get; set; } = null!;

    /// <summary>Producto / Categoria.</summary>
    public string TipoElemento { get; set; } = null!;
    public int IdElemento { get; set; }
    public string NombreElemento { get; set; } = null!;

    /// <summary>Categoria del elemento (la suya si es una categoria): sirve para el color y la inicial en la interfaz.</summary>
    public int IdCategoriaElemento { get; set; }
    public string CategoriaElemento { get; set; } = null!;

    /// <summary>Creado / Editado / Reordenado / Habilitado / Deshabilitado.</summary>
    public string Accion { get; set; } = null!;

    /// <summary>Nombre / Precio / Categoria / Posicion. El precio llega como numero sin formato.</summary>
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? Motivo { get; set; }
}

/// <summary>Administrador que aparece en el historial; alimenta el filtro por usuario.</summary>
public class UsuarioHistorialResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = null!;
}
