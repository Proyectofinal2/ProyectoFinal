namespace MonteCarlo.WEB.Models.Reservas;

/// <summary>Elemento del catalogo real de estados devuelto por la API.</summary>
public class EstadoReservaReporteApiResponse
{
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Fila de la distribucion por estado del reporte historico.</summary>
public class DistribucionEstadoReservaApiResponse
{
    public string Estado { get; set; } = string.Empty;
    public int CantidadReservas { get; set; }
    public decimal Porcentaje { get; set; }
}

/// <summary>Indicadores historicos de reservas por periodo.</summary>
public class ReporteReservasApiResponse
{
    public int TotalReservas { get; set; }
    public int PersonasReservadas { get; set; }
    public int ReservasAtendidas { get; set; }
    public int NoShows { get; set; }
    public int Cancelaciones { get; set; }
    public decimal? PorcentajeAsistencia { get; set; }
    public string? MensajeAsistencia { get; set; }
    public List<DistribucionEstadoReservaApiResponse> DistribucionPorEstado { get; set; } = [];
}
