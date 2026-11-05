namespace MonteCarlo.WEB.Models.Mesas;

/// <summary>Tal como lo devuelve GET api/mesas -- alimenta ambos modos del canvas (HU-MES-002, HU-OPE-004).</summary>
public class MesaApiResponse
{
    public int IdMesa { get; set; }
    public int NumeroMesa { get; set; }
    public int Capacidad { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public string Estado { get; set; } = "Disponible";
    public int? PersonasActuales { get; set; }
    public bool EsProxima { get; set; }
    public int? MinutosProximaReserva { get; set; }
    public int? MinutosHastaSiguienteReserva { get; set; }
    public bool TieneReservaActiva { get; set; }
    public bool EnUnionActiva { get; set; }
    public int DuracionReservaMinutos { get; set; }
    public MesaReservaActualApiResponse? ReservaActual { get; set; }
    public bool EsperandoLlegada { get; set; }
    public MesaProximaReservaApiResponse? ProximaReserva { get; set; }
    public MesaCuentaApiResponse? Cuenta { get; set; }
}

public class MesaReservaActualApiResponse
{
    public int IdReserva { get; set; }
    public string? CodigoReserva { get; set; }
    public string Cliente { get; set; } = "Walk-in";
    public int Personas { get; set; }
    public TimeOnly HoraReserva { get; set; }
    public DateTime? FechaAtencion { get; set; }
    public DateTime OcupadaDesde { get; set; }
    public int MinutosSentados { get; set; }
}

public class MesaProximaReservaApiResponse
{
    public int IdReserva { get; set; }
    public string Cliente { get; set; } = "";
    public int Personas { get; set; }
    public TimeOnly HoraReserva { get; set; }
}

public class MesaCuentaApiResponse
{
    public decimal Total { get; set; }
    public int Productos { get; set; }
    public string Version { get; set; } = "";

    /// <summary>Paso el tiempo de uso de la mesa (llegada + duracion del turno) mas la tolerancia para cobrar.</summary>
    public bool Antigua { get; set; }
    public DateTime VenceDesde { get; set; }
}

/// <summary>Datos de GET api/mesas/resumen para los indicadores del panel.</summary>
public class ResumenMesasApiResponse
{
    public int MesasHabilitadas { get; set; }
    public int MesasDisponibles { get; set; }
    public int MesasOcupadas { get; set; }
    public int CuentasAbiertas { get; set; }
    public decimal ConsumoEnCurso { get; set; }
    public int CuentasAntiguas { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public string? MensajeContextual { get; set; }
}

public class SugerenciaWalkInApiResponse
{
    public List<int> NumerosMesa { get; set; } = [];
    public bool EsCombinacionDeMesas { get; set; }
    public bool RequiereRevisionManual { get; set; }
}

public class ReservaWalkInApiResponse
{
    public int IdReserva { get; set; }
    public List<int> NumerosMesa { get; set; } = [];
    public bool EsCombinacionDeMesas { get; set; }
}

/// <summary>Pasa el rol al canvas: oculta "Editar salon" y el boton "+" para el rol Regular (validado tambien en el servidor).</summary>
public class MesasIndexViewModel
{
    public bool EsAdministradorGeneral { get; set; }
}
