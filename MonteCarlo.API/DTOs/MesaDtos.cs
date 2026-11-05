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

    /// <summary>Solo si Estado == "Ocupada": la reserva que ocupa la mesa, para abrir su cuenta (HU-CON-002).</summary>
    public MesaReservaActualResponse? ReservaActual { get; set; }

    /// <summary>
    /// Reserva Confirmada sin llegada registrada cuya hora ya paso: sigue contando como Ocupada para la
    /// disponibilidad, pero la pantalla ofrece "Cliente llegó" o "No Show" en vez de la cuenta.
    /// </summary>
    public bool EsperandoLlegada { get; set; }

    /// <summary>Siguiente reserva por llegar dentro de la ventana de alerta (mesa Disponible o Ocupada).</summary>
    public MesaProximaReservaResponse? ProximaReserva { get; set; }

    /// <summary>Cuenta abierta de la reserva actual; null mientras todavia no hay pedidos. En una union las dos mesas comparten la misma.</summary>
    public MesaCuentaResponse? Cuenta { get; set; }
}

public class MesaReservaActualResponse
{
    public int IdReserva { get; set; }

    /// <summary>Null en un walk-in.</summary>
    public string? CodigoReserva { get; set; }

    /// <summary>"Walk-in" si no hay cliente real.</summary>
    public string Cliente { get; set; } = null!;
    public int Personas { get; set; }
    public TimeOnly HoraReserva { get; set; }

    /// <summary>Llegada del cliente; null mientras se espera.</summary>
    public DateTime? FechaAtencion { get; set; }

    /// <summary>Inicio de la ocupacion: la llegada, o la hora de la reserva si todavia no llega. Sirve para el tiempo sentado.</summary>
    public DateTime OcupadaDesde { get; set; }

    /// <summary>Minutos desde <see cref="OcupadaDesde"/> con el reloj del servidor; la pantalla no usa el del navegador (la hora la define el servidor).</summary>
    public int MinutosSentados { get; set; }
}

public class MesaProximaReservaResponse
{
    public int IdReserva { get; set; }
    public string Cliente { get; set; } = null!;
    public int Personas { get; set; }
    public TimeOnly HoraReserva { get; set; }
}

public class MesaCuentaResponse
{
    public decimal Total { get; set; }

    /// <summary>Unidades activas en la cuenta.</summary>
    public int Productos { get; set; }

    /// <summary>RowVersion en base64.</summary>
    public string Version { get; set; } = null!;

    /// <summary>Paso el tiempo de uso de la mesa (llegada + duracion del turno) mas el margen (CuentaVencimiento): ya solo queda cobrarla.</summary>
    public bool Antigua { get; set; }

    /// <summary>Desde cuando es antigua; sirve para el aviso «Esta cuenta venció el...».</summary>
    public DateTime VenceDesde { get; set; }
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

    /// <summary>Cuentas abiertas antiguas (por reserva: una union de dos mesas cuenta una vez).</summary>
    public int CuentasAntiguas { get; set; }

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
