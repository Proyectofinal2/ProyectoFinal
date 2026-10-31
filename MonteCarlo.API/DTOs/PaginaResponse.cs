namespace MonteCarlo.API.DTOs;

/// <summary>Una pagina de resultados con el total para armar la paginacion.</summary>
public class PaginaResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}
