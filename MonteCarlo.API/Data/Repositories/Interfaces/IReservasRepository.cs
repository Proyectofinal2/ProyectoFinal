using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IReservasRepository
{
    /// <summary>Reserva con cliente, mesa y estado cargados (entidad rastreada).</summary>
    Task<Reserva?> ObtenerPorCodigoAsync(string codigo);

    /// <summary>Reserva por Id (entidad rastreada), para las acciones administrativas de asignación de mesa.</summary>
    Task<Reserva?> ObtenerPorIdAsync(int id);

    Task<EstadoReserva?> ObtenerEstadoAsync(string nombre);

    /// <summary>Catalogo de estados para filtros y distribuciones de reportes.</summary>
    Task<List<EstadoReservaReporteResponse>> ObtenerEstadosReporteAsync();

    /// <summary>Agregados de reservas por periodo, sin materializar entidades ni relaciones.</summary>
    Task<ResumenReporteReservasDatos> ObtenerReporteAsync(DateOnly fechaInicio, DateOnly fechaFin, string? estado);

    Task<List<byte>> ObtenerDiasCierreFijoAsync();
    Task<List<DateOnly>> ObtenerFechasCierreEventualAsync(DateOnly desde);
    Task<List<HorarioOperacion>> ObtenerHorariosOperacionAsync();

    /// Reservas de una fecha con cliente, estado y mesas, ordenadas por hora (entidades rastreadas).
    Task<List<Reserva>> ObtenerPorFechaAsync(DateOnly fecha);

    /// <summary>Reservas de un rango de fechas (ambas incluidas), ordenadas por fecha y hora.</summary>
    Task<List<Reserva>> ObtenerPorRangoAsync(DateOnly desde, DateOnly hasta);

    /// <summary>HU-OPE-005: reservas de hoy en adelante cuyo nombre o teléfono coincidan con el término.</summary>
    Task<List<Reserva>> BuscarPorNombreOTelefonoAsync(string termino, int limite);

    /// <summary>Verifica si ya existe una reserva con ese código (evita duplicados).</summary>
    Task<bool> ExisteCodigoAsync(string codigo);

    /// <summary>
    /// Reservas Confirmadas que pueden ocupar una mesa ahora: las de hoy y las de dias anteriores que
    /// siguen con la cuenta abierta (la mesa sigue ocupada hasta cobrar). Con sus mesas, sin rastrear.
    /// </summary>
    Task<List<Reserva>> ObtenerConfirmadasParaMesasAsync(DateOnly hoy);

    /// <summary>Reserva por Id con cliente, estado y mesas (con su mesa), rastreada, para la cuenta y el cobro (HU-CON-002).</summary>
    Task<Reserva?> ObtenerParaCuentaAsync(int idReserva);

    /// <summary>
    /// Reserva Confirmada y atendida que ocupa esa mesa, para liberarla (HU-OPE-004): la de la fecha indicada
    /// o una de otro dia con la cuenta todavia abierta.
    /// </summary>
    Task<Reserva?> ObtenerConfirmadaActivaPorMesaAsync(int idMesa, DateOnly fecha);

    /// <summary>Agrega una nueva reserva al contexto (HU-RES-003).</summary>
    void Agregar(Reserva reserva);

    Task GuardarCambiosAsync();
}
