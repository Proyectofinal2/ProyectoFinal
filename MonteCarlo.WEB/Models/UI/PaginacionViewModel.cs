namespace MonteCarlo.WEB.Models.UI;

/// <summary>
/// Datos para el parcial _Paginacion. Los enlaces conservan los filtros de la pantalla: cada pagina es una URL
/// (GET), asi que el boton Atras y los enlaces compartidos funcionan.
/// </summary>
public class PaginacionViewModel
{
    public int Pagina { get; init; } = 1;
    public int TotalPaginas { get; init; } = 1;
    public int Total { get; init; }
    public int TamanoPagina { get; init; }

    /// <summary>Lo que se cuenta, en plural: "cambios", "productos".</summary>
    public string Sustantivo { get; init; } = "registros";

    public required string Accion { get; init; }
    public required string Controlador { get; init; }

    /// <summary>Filtros activos; los vacios no viajan en la URL.</summary>
    public Dictionary<string, string?> Parametros { get; init; } = [];

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
    public int Hasta => Math.Min(Total, Pagina * TamanoPagina);
    public bool TieneAnterior => Pagina > 1;
    public bool TieneSiguiente => Pagina < TotalPaginas;

    /// <summary>Primera, ultima y la vecindad de la actual; null representa los puntos suspensivos.</summary>
    public List<int?> Numeros()
    {
        var numeros = new List<int?>();
        var anterior = 0;

        for (var i = 1; i <= TotalPaginas; i++)
        {
            if (i != 1 && i != TotalPaginas && Math.Abs(i - Pagina) > 1)
                continue;

            if (anterior != 0 && i - anterior > 1)
                numeros.Add(null);

            numeros.Add(i);
            anterior = i;
        }

        return numeros;
    }
}
