using System.Globalization;

namespace MonteCarlo.WEB.Models.Catalogo;

// Respuestas de la API del catalogo (HU-CON-001), tal como llegan en el campo "data".

/// <summary>GET api/productos.</summary>
public class ProductoApiResponse
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public bool Disponible { get; set; }
    public int IdCategoria { get; set; }
    public string Categoria { get; set; } = "";

    /// <summary>false si la categoria esta deshabilitada: el producto no se puede vender aunque este disponible.</summary>
    public bool CategoriaActiva { get; set; }

    /// <summary>true si no tiene ventas asociadas y se puede eliminar.</summary>
    public bool Eliminable { get; set; }
}

/// <summary>GET api/categorias-producto: ya en el orden del menu.</summary>
public class CategoriaProductoApiResponse
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool Activa { get; set; }
    public int TotalProductos { get; set; }
    public int ProductosDisponibles { get; set; }

    public int ProductosDeshabilitados => TotalProductos - ProductosDisponibles;
}

/// <summary>Resultado de habilitar o deshabilitar una categoria.</summary>
public class CambioEstadoCategoriaApiResponse
{
    public int IdCategoria { get; set; }
    public bool Activa { get; set; }
    public int ProductosAfectados { get; set; }
}

/// <summary>Una fila del historial de cambios de productos y categorias.</summary>
public class HistorialCatalogoApiResponse
{
    public int IdHistorialCatalogo { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "";

    /// <summary>General / Regular.</summary>
    public string RolUsuario { get; set; } = "";

    public string RolLegible => string.IsNullOrEmpty(RolUsuario) ? "" : $"Administrador {RolUsuario}";

    /// <summary>Producto / Categoria.</summary>
    public string TipoElemento { get; set; } = "";
    public int IdElemento { get; set; }
    public string NombreElemento { get; set; } = "";

    /// <summary>Categoria del elemento (la suya si es una categoria): da el color y la inicial.</summary>
    public int IdCategoriaElemento { get; set; }
    public string CategoriaElemento { get; set; } = "";

    public string Accion { get; set; } = "";
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? Motivo { get; set; }

    public bool EsCategoria => TipoElemento == "Categoria";

    /// <summary>Etiqueta legible del campo (la API guarda claves sin tildes).</summary>
    public string CampoLegible => Campo switch
    {
        "Categoria" => "Categoría",
        "Posicion" => "Posición",
        _ => Campo ?? ""
    };

    /// <summary>El precio llega como numero plano; se muestra en colones.</summary>
    public string FormatearValor(string? valor) =>
        Campo == "Precio" && decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto)
            ? Colones.Formato(monto)
            : valor ?? "";
}

/// <summary>Administrador que aparece en el historial (alimenta el filtro por usuario).</summary>
public class UsuarioHistorialApiResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = "";
}

/// <summary>Pagina de resultados con el total para armar la paginacion.</summary>
public class PaginaApiResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}
