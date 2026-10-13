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
