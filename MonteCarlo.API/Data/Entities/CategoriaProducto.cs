namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Categoria del menu (HU-CON-001). Cualquier administrador las crea, renombra,
/// reordena y deshabilita; solo se elimina una sin productos. Deshabilitar una categoria deshabilita
/// todos sus productos (regla de la aplicacion). El color de la interfaz se deriva
/// de IdCategoria, no se guarda.
/// </summary>
public class CategoriaProducto
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = null!;

    /// <summary>Posicion en el menu y en el selector de consumo (1..N). No es unico.</summary>
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public ICollection<HistorialCatalogo> Historial { get; set; } = new List<HistorialCatalogo>();
}
