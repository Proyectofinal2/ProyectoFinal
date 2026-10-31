using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MonteCarlo.WEB.Models.Catalogo;

/// <summary>Pestana Productos (HU-CON-001): filtros, categorias con sus conteos y la lista ya filtrada.</summary>
public class ProductosIndexViewModel
{
    public string? Busqueda { get; set; }
    public int? IdCategoria { get; set; }

    /// <summary>"" = todos, "disponible" o "deshabilitado".</summary>
    public string? Estado { get; set; }

    public List<CategoriaProductoApiResponse> Categorias { get; set; } = [];
    public List<ProductoApiResponse> Productos { get; set; } = [];

    /// <summary>Si viene, el panel lateral se abre con este formulario (alta o edicion con errores de validacion).</summary>
    public ProductoFormViewModel? Formulario { get; set; }

    public int TotalProductos => Categorias.Sum(c => c.TotalProductos);

    public bool TieneFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda) || IdCategoria.HasValue || !string.IsNullOrEmpty(Estado);

    /// <summary>Un grupo por categoria con productos, en el orden del menu.</summary>
    public List<GrupoProductosViewModel> Grupos => Categorias
        .Select(c => new GrupoProductosViewModel
        {
            Categoria = c,
            Productos = Productos.Where(p => p.IdCategoria == c.IdCategoria).ToList()
        })
        .Where(g => g.Productos.Count > 0)
        .ToList();
}

public class GrupoProductosViewModel
{
    public required CategoriaProductoApiResponse Categoria { get; init; }
    public List<ProductoApiResponse> Productos { get; init; } = [];
}

/// <summary>Formulario del panel lateral para crear o editar un producto.</summary>
public class ProductoFormViewModel
{
    /// <summary>0 = producto nuevo.</summary>
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre del producto.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    /// <summary>Texto: se acepta «3500», «3 500», «3.500» o «3500,50». Ver <see cref="Colones"/>.</summary>
    [Required(ErrorMessage = "Ingresa el precio en colones.")]
    [Colones]
    [Display(Name = "Precio (₡ colones)")]
    public string Precio { get; set; } = "";

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public int? IdCategoria { get; set; }

    /// <summary>Categorias que se pueden elegir: las activas y, al editar, la actual aunque este deshabilitada.</summary>
    [ValidateNever]
    public List<CategoriaProductoApiResponse> Categorias { get; set; } = [];

    /// <summary>Solo para el Administrador General; null si no puede verlo.</summary>
    [ValidateNever]
    public List<HistorialCatalogoApiResponse>? Historial { get; set; }

    public bool EsEdicion => IdProducto > 0;
}

/// <summary>Pestana Categorias: lista en el orden del menu, con la fila en edicion si la hay.</summary>
public class CategoriasIndexViewModel
{
    public List<CategoriaProductoApiResponse> Categorias { get; set; } = [];

    /// <summary>Categoria cuyo nombre se esta editando en su propia fila.</summary>
    public int? EditarId { get; set; }

    /// <summary>Muestra una fila vacia al final para crear una categoria.</summary>
    public bool Nueva { get; set; }

    public string? NombreEdicion { get; set; }
    public string? ErrorEdicion { get; set; }
}

/// <summary>Pestana Historial de cambios (solo Administrador General): filtros y una pagina de resultados.</summary>
public class HistorialCatalogoViewModel
{
    public string? Busqueda { get; set; }
    public string? Tipo { get; set; }
    public int? IdUsuario { get; set; }
    public string? Accion { get; set; }

    public List<UsuarioHistorialApiResponse> Usuarios { get; set; } = [];
    public PaginaApiResponse<HistorialCatalogoApiResponse> Pagina { get; set; } = new();

    public bool TieneFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda) || !string.IsNullOrEmpty(Tipo) || IdUsuario.HasValue || !string.IsNullOrEmpty(Accion);
}
