using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>Categoria del menu con sus conteos (HU-CON-001). La lista llega ya en el orden del menu.</summary>
public class CategoriaProductoResponse
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = null!;
    public int Orden { get; set; }
    public bool Activa { get; set; }
    public int TotalProductos { get; set; }
    public int ProductosDisponibles { get; set; }
}

/// <summary>Datos para crear o renombrar una categoria.</summary>
public class GuardarCategoriaProductoRequest
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    public string? Nombre { get; set; }
}

/// <summary>Resultado de habilitar o deshabilitar una categoria: cuantos productos cambiaron con ella.</summary>
public class CambioEstadoCategoriaResponse
{
    public int IdCategoria { get; set; }
    public bool Activa { get; set; }
    public int ProductosAfectados { get; set; }
}
