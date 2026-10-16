namespace MonteCarlo.API.DTOs;

/// <summary>Datos de una reserva que se muestran al cliente que consulta con su código.</summary>
public class ReservaResponse
{
    public string CodigoReserva { get; set; } = null!;
    public string NombreCliente { get; set; } = null!;
    public DateOnly FechaReserva { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = null!;

    /// <summary>Solo Pendiente y Confirmada pueden cancelarse (HU-RES-005).</summary>
    public bool PuedeCancelarse { get; set; }

    /// <summary>Mesa(s) asignadas; vacio si aun no se asigno (Pendiente/RequiereAsignacionManual).</summary>
    public List<int> NumerosMesa { get; set; } = [];
}

public class DisponibilidadFechaResponse
{
    public DateOnly Fecha { get; set; }
    public bool Disponible { get; set; }
    public string? MotivoNoDisponible { get; set; }
    public List<TimeOnly> HorariosDisponibles { get; set; } = [];
}

public class DisponibilidadMesResponse
{
    public int CantidadPersonas { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public List<DisponibilidadFechaResponse> Fechas { get; set; } = [];
}

/// <summary>Resultado del algoritmo de combinacion de mesas (HU-MES-005/006), sin aplicar.</summary>
public class AsignacionSugeridaResponse
{
    public List<int> NumerosMesa { get; set; } = [];
    public bool EsCombinacionDeMesas { get; set; }
    public bool RequiereRevisionManual { get; set; }
}

/// <summary>Mesas elegidas por el admin para asignar o reasignar una reserva (HU-MES-005/006).</summary>
public class AsignarMesaRequest
{
    public List<int> IdsMesa { get; set; } = [];
}

/// <summary>Resultado de aplicar una asignacion/reasignacion manual de mesa.</summary>
public class AsignacionMesaResponse
{
    public List<int> NumerosMesa { get; set; } = [];
    public bool EsCombinacionDeMesas { get; set; }
    public string Estado { get; set; } = null!;
}

public class ReservaDelDiaResponse
{
    public int IdReserva { get; set; }
    public string? CodigoReserva { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public string NombreCliente { get; set; } = null!;
    public string? Telefono { get; set; }
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = null!;
    public List<int> NumerosMesa { get; set; } = [];
    public string? Observaciones { get; set; }
    public bool EsWalkIn { get; set; }
}
