namespace MonteCarlo.API.DTOs;

/// <summary>Elemento del catalogo real de estados de reserva.</summary>
public class EstadoReservaReporteResponse
{
    /// <summary>Nombre configurado para el estado.</summary>
    public string Nombre { get; set; } = null!;
}

/// <summary>Distribucion de reservas por su estado actual.</summary>
public class DistribucionEstadoReservaResponse
{
    /// <summary>Estado actual de las reservas contabilizadas.</summary>
    public string Estado { get; set; } = null!;

    /// <summary>Cantidad de reservas del conjunto mostrado.</summary>
    public int CantidadReservas { get; set; }

    /// <summary>Participacion sobre el total del conjunto mostrado.</summary>
    public decimal Porcentaje { get; set; }
}

/// <summary>Reporte historico de reservas para un periodo y estado opcional.</summary>
public class ReporteReservasResponse
{
    /// <summary>Cantidad de reservas en el periodo y filtro aplicados.</summary>
    public int TotalReservas { get; set; }

    /// <summary>Suma de personas de las reservas mostradas.</summary>
    public int PersonasReservadas { get; set; }

    /// <summary>Reservas con fecha de atencion registrada.</summary>
    public int ReservasAtendidas { get; set; }

    /// <summary>Reservas cuyo estado actual es No Show.</summary>
    public int NoShows { get; set; }

    /// <summary>Reservas cuyo estado actual es Cancelada.</summary>
    public int Cancelaciones { get; set; }

    /// <summary>Asistencia calculada cuando el conjunto permite interpretarla.</summary>
    public decimal? PorcentajeAsistencia { get; set; }

    /// <summary>Motivo por el que la asistencia no aplica o no tiene datos.</summary>
    public string? MensajeAsistencia { get; set; }

    /// <summary>Filas de distribucion por los estados del catalogo.</summary>
    public List<DistribucionEstadoReservaResponse> DistribucionPorEstado { get; set; } = [];
}

/// <summary>Proyeccion agregada que usa el repositorio para evitar cargar reservas completas.</summary>
public class ResumenReporteReservasDatos
{
    /// <summary>Total agregado de reservas.</summary>
    public int TotalReservas { get; set; }

    /// <summary>Total agregado de personas.</summary>
    public int PersonasReservadas { get; set; }

    /// <summary>Total agregado de atenciones.</summary>
    public int ReservasAtendidas { get; set; }

    /// <summary>Total agregado de No Shows.</summary>
    public int NoShows { get; set; }

    /// <summary>Total agregado de cancelaciones.</summary>
    public int Cancelaciones { get; set; }

    /// <summary>Conteos agrupados por estado.</summary>
    public List<ConteoEstadoReservaDatos> ConteosPorEstado { get; set; } = [];
}

/// <summary>Conteo SQL agrupado por estado, previo al calculo de porcentajes.</summary>
public class ConteoEstadoReservaDatos
{
    /// <summary>Nombre del estado agrupado.</summary>
    public string Estado { get; set; } = null!;

    /// <summary>Cantidad agregada de reservas.</summary>
    public int CantidadReservas { get; set; }
}
