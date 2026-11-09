using System.ComponentModel.DataAnnotations;
using MonteCarlo.WEB.Models.Mesas;
using MonteCarlo.WEB.Models.Reservas;
using MonteCarlo.WEB.Models.Ventas;

namespace MonteCarlo.WEB.Models.Panel;

/// <summary>Periodos comunes que reutilizaran los futuros indicadores del dashboard.</summary>
public enum PeriodoDashboard
{
    [Display(Name = "Hoy")]
    Hoy = 0,

    [Display(Name = "Ultimos 7 dias")]
    UltimosSieteDias = 1,

    [Display(Name = "Este mes")]
    EsteMes = 2
}

/// <summary>
/// Filtros globales reutilizables del dashboard y sus reportes.
/// </summary>
public class FiltrosDashboardViewModel
{
    [Display(Name = "Periodo")]
    public PeriodoDashboard Periodo { get; set; } = PeriodoDashboard.Hoy;

    public string? EstadoReserva { get; set; }

    public (DateOnly FechaInicio, DateOnly FechaFin) ResolverRango(DateOnly hoy)
    {
        switch (Periodo)
        {
            case PeriodoDashboard.Hoy:
                return (hoy, hoy);
            case PeriodoDashboard.UltimosSieteDias:
                return (hoy.AddDays(-6), hoy);
            case PeriodoDashboard.EsteMes:
                return (new DateOnly(hoy.Year, hoy.Month, 1), hoy);
            default:
                Periodo = PeriodoDashboard.Hoy;
                return (hoy, hoy);
        }
    }
}

/// <summary>Periodo exclusivo de los indicadores historicos de ventas.</summary>
public enum PeriodoVentasDashboard
{
    [Display(Name = "Hoy")]
    Hoy = 0,

    [Display(Name = "Esta semana")]
    EstaSemana = 1,

    [Display(Name = "Este mes")]
    EsteMes = 2
}

/// <summary>
/// Usa limites DateTime [inicio, ahora) porque los cobros se registran con fecha y hora.
/// Es independiente del filtro de reservas para no alterar su rango historico actual.
/// </summary>
public class FiltrosVentasDashboardViewModel
{
    [Display(Name = "Periodo")]
    public PeriodoVentasDashboard Periodo { get; set; } = PeriodoVentasDashboard.Hoy;

    public (DateTime FechaInicio, DateTime FechaFinExclusiva) ResolverRango(DateTime ahora)
    {
        var inicio = Periodo switch
        {
            PeriodoVentasDashboard.Hoy => ahora.Date,
            PeriodoVentasDashboard.EstaSemana => ahora.Date.AddDays(-((int)ahora.DayOfWeek + 6) % 7),
            PeriodoVentasDashboard.EsteMes => new DateTime(ahora.Year, ahora.Month, 1),
            _ => ahora.Date
        };

        if (!Enum.IsDefined(Periodo))
            Periodo = PeriodoVentasDashboard.Hoy;

        return (inicio, ahora);
    }
}

/// <summary>Modelo integrado del panel administrativo y su analisis historico.</summary>
public class PanelIndexViewModel
{
    public FiltrosDashboardViewModel Filtros { get; init; } = new();
    public ResumenMesasApiResponse? ResumenMesas { get; init; }
    public DateTime? FechaActualizacion { get; init; }
    public string? ErrorResumen { get; init; }
    public IReadOnlyList<EstadoReservaReporteApiResponse> EstadosReserva { get; init; } = [];
    public ReporteReservasApiResponse? ReporteReservas { get; init; }
    public string? ErrorReporte { get; init; }
    public bool TieneFiltroEstado => !string.IsNullOrWhiteSpace(Filtros.EstadoReserva);
    public FiltrosVentasDashboardViewModel FiltrosVentas { get; init; } = new();
    public ResumenVentasDashboardApiResponse? ResumenVentas { get; init; }
    public string? ErrorResumenVentas { get; init; }
}
