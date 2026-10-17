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
}

/// Reservas de un dia para planificar la operacion.
public class ReservasDelDiaViewModel
{
    public DateOnly Fecha { get; set; }
    public List<ReservaDelDiaApiResponse> Reservas { get; set; } = [];

    /// <summary>HU-OPE-005: termino de busqueda por nombre o telefono.</summary>
    public string? TerminoBusqueda { get; set; }

    /// <summary>True cuando el usuario esta en modo busqueda (HU-OPE-005).</summary>
    public bool ModoBusqueda => !string.IsNullOrWhiteSpace(TerminoBusqueda);

    public int PersonasEsperadas =>
        Reservas.Where(r => r.Estado is "Pendiente" or "Confirmada").Sum(r => r.CantidadPersonas);

    public int Pendientes => Reservas.Count(r => r.Estado == "Pendiente");
}