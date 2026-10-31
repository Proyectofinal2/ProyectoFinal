namespace MonteCarlo.WEB.Models.Catalogo;

/// <summary>
/// Identidad visual de una categoria. No se guarda: el color se deriva del id (6 tonos que se repiten)
/// y el icono es la inicial del nombre. Las clases viven en site.css (.mc-cat-1 ... .mc-cat-6).
/// </summary>
public static class CategoriaEstilo
{
    public const int Colores = 6;

    /// <summary>Con id 0 (categoria eliminada, sin id) devuelve el estilo neutro.</summary>
    public static string Clase(int idCategoria) =>
        idCategoria <= 0 ? "mc-cat-off" : $"mc-cat-{(idCategoria - 1) % Colores + 1}";

    public static string Inicial(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? "?" : nombre.Trim()[..1].ToUpperInvariant();
}
