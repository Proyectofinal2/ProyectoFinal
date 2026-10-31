using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>Producto del catalogo (HU-CON-001). El precio esta en colones.</summary>
public class ProductoResponse
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public bool Disponible { get; set; }
    public int IdCategoria { get; set; }
    public string Categoria { get; set; } = null!;

    /// <summary>false si la categoria esta deshabilitada: el producto no se puede vender aunque este disponible.</summary>
    public bool CategoriaActiva { get; set; }

    /// <summary>true si no tiene ninguna venta asociada y por tanto se puede eliminar (solo se informa en consultas).</summary>
    public bool Eliminable { get; set; }
}

/// <summary>Datos para crear o editar un producto.</summary>
public class GuardarProductoRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string? Nombre { get; set; }

    /// <summary>Precio en colones, con hasta 2 decimales.</summary>
    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0, 99999999.99, ErrorMessage = "El precio debe estar entre 0 y 99 999 999,99 colones.")]
    public decimal? Precio { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public int? IdCategoria { get; set; }
}
