namespace MonteCarlo.WEB.Models.Reservas;

/// Reserva tal como la devuelve GET api/reservas/del-dia y GET api/reservas/buscar.
public class ReservaDelDiaApiResponse
{
    public int IdReserva { get; set; }
    public string? CodigoReserva { get; set; }
    public DateOnly FechaReserva { get; set; }  // ⬅️ NUEVO
    public TimeOnly HoraReserva { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<int> NumerosMesa { get; set; } = [];
    public string? Observaciones { get; set; }
    public bool EsWalkIn { get; set; }
    public DateTime? FechaAtencion { get; set; }
    public ConsumoReservaApiResponse? Consumo { get; set; }

    /// <summary>Confirmada sin llegada cuya hora ya paso: minutos de retraso.</summary>
    public int? MinutosDeRetraso { get; set; }

    /// <summary>Cliente sentado que supero el tiempo de la reserva: minutos de exceso.</summary>
    public int? MinutosExcedidos { get; set; }

    /// <summary>Confirmada con la llegada registrada: se muestra como «En progreso» (el estado guardado sigue Confirmada).</summary>
    public bool EnProgreso => Estado == "Confirmada" && FechaAtencion is not null;
}

/// Venta de la reserva (GET api/reservas/del-dia): sirve para la columna Consumo.
public class ConsumoReservaApiResponse
{
    public int IdVenta { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Productos { get; set; }
}

/// Reservas de un rango de fechas (por defecto, hoy) para planificar la operacion.
public class ReservasDelDiaViewModel
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }

    /// <summary>El rango abarca mas de un dia: la tabla muestra la columna Fecha.</summary>
    public bool EsRango => Hasta > Desde;
    public List<ReservaDelDiaApiResponse> Reservas { get; set; } = [];

    public const int TamanoPagina = 10;

    /// <summary>Filtro por origen: vacio (todas), "reserva" (clientes con reserva) o "walkin".</summary>
    public string Tipo { get; set; } = "";

    public int Pagina { get; set; } = 1;

    /// <summary>Con la llegada registrada (en progreso o ya finalizadas).</summary>
    public int Atendidas => Reservas.Count(r => r.FechaAtencion is not null);

    /// <summary>Aun por llegar y a tiempo: Confirmadas o Pendientes sin llegada y sin retraso.</summary>
    public int Proximas => Reservas.Count(r => r.Estado is "Confirmada" or "Pendiente" && r.FechaAtencion is null && r.MinutosDeRetraso is null);

    /// <summary>Confirmadas cuya hora ya paso y el cliente no ha llegado.</summary>
    public int ConRetraso => Reservas.Count(r => r.MinutosDeRetraso is not null);

    public int ConReserva => Reservas.Count(r => !r.EsWalkIn);
    public int WalkIn => Reservas.Count(r => r.EsWalkIn);

    /// <summary>Las reservas del rango con el filtro de origen aplicado (antes de paginar).</summary>
    public List<ReservaDelDiaApiResponse> Filtradas => Tipo switch
    {
        "walkin" => Reservas.Where(r => r.EsWalkIn).ToList(),
        "reserva" => Reservas.Where(r => !r.EsWalkIn).ToList(),
        _ => Reservas
    };

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Filtradas.Count / (double)TamanoPagina));

    public List<ReservaDelDiaApiResponse> PaginaActual =>
        Filtradas.Skip((Pagina - 1) * TamanoPagina).Take(TamanoPagina).ToList();

    /// <summary>HU-OPE-005: termino de busqueda por nombre o telefono.</summary>
    public string? TerminoBusqueda { get; set; }

    /// <summary>True cuando el usuario esta en modo busqueda (HU-OPE-005).</summary>
    public bool ModoBusqueda => !string.IsNullOrWhiteSpace(TerminoBusqueda);
}