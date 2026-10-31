namespace MonteCarlo.API.DTOs;

/// <summary>Lectura del canvas de mesas; "Ocupada" nunca se persiste (ver MesasService.MapearEstadoEnVivo).</summary>
public class MesaResponse
{
    public int IdMesa { get; set; }
    public int NumeroMesa { get; set; }
    public int Capacidad { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }

    /// <summary>"Disponible" | "Ocupada" | "Deshabilitada".</summary>
    public string Estado { get; set; } = "Disponible";

    /// <summary>Solo si Estado == "Ocupada": personas sentadas actualmente.</summary>
    public int? PersonasActuales { get; set; }

    /// <summary>Mesa Disponible con una reserva Confirmada a punto de llegar; bloquea el walk-in.</summary>
    public bool EsProxima { get; set; }
    public int? MinutosProximaReserva { get; set; }

    /// <summary>Mesa Ocupada con otra reserva Confirmada por llegar encima (badge de reloj).</summary>
    public int? MinutosHastaSiguienteReserva { get; set; }

    /// <summary>Bloquea deshabilitar/eliminar (HU-MES-003): tiene alguna reserva Confirmada vigente.</summary>
    public bool TieneReservaActiva { get; set; }

    /// <summary>Parte de una combinacion de 2 mesas para la misma reserva Confirmada.</summary>
    public bool EnUnionActiva { get; set; }

    /// <summary>Mismo valor en todas las mesas; evita un segundo round-trip desde el canvas.</summary>
    public int DuracionReservaMinutos { get; set; }
}

/// <summary>Indicadores actuales de mesas para el panel administrativo.</summary>
public class ResumenMesasResponse
{
    /// <summary>Total de mesas cuyo estado persistido permite operar.</summary>
    public int MesasHabilitadas { get; set; }

    /// <summary>Mesas habilitadas sin una ocupacion en curso.</summary>
    public int MesasDisponibles { get; set; }

    /// <summary>Mesas habilitadas con una ocupacion en curso.</summary>
    public int MesasOcupadas { get; set; }

    /// <summary>Porcentaje de mesas ocupadas sobre las habilitadas.</summary>
    public decimal PorcentajeOcupacion { get; set; }

    /// <summary>Se informa cuando no hay mesas habilitadas para calcular la ocupacion.</summary>
    public string? MensajeContextual { get; set; }
}

// HU-MES-001/002/003/006: CRUD minimo de mesas, alcance del
// companero de equipo (ver MesasService para el detalle).

public class CrearMesaRequest
{
    public int NumeroMesa { get; set; }
    public int Capacidad { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
}

public class ActualizarCapacidadRequest
{
    public int Capacidad { get; set; }
}

/// <summary>"Disponible" o "Deshabilitada" unicamente -- "Ocupada" nunca se asigna a mano.</summary>
public class ActualizarEstadoRequest
{
    public string Estado { get; set; } = null!;
}

public class ActualizarPosicionRequest
{
    public int PosX { get; set; }
    public int PosY { get; set; }
}

// ============================================================
// HU-OPE-004 (walk-in)
// ============================================================

/// <summary>POST /api/reservas/walkin: 1 o 2 mesas elegidas por el admin (manual o por la recomendacion aceptada).</summary>
public class CrearReservaWalkInRequest
{
    public int CantidadPersonas { get; set; }
    public List<int> IdsMesa { get; set; } = [];
}

public class ReservaWalkInResponse
{
    public int IdReserva { get; set; }
    public List<int> NumerosMesa { get; set; } = [];
    public bool EsCombinacionDeMesas { get; set; }
}
