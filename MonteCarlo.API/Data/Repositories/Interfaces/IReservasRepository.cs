using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IReservasRepository
{
    /// <summary>Reserva con cliente, mesa y estado cargados (entidad rastreada).</summary>
    Task<Reserva?> ObtenerPorCodigoAsync(string codigo);

    /// <summary>Reserva por Id (entidad rastreada), para las acciones administrativas de asignación de mesa.</summary>
    Task<Reserva?> ObtenerPorIdAsync(int id);

    Task<EstadoReserva?> ObtenerEstadoAsync(string nombre);

    Task<List<byte>> ObtenerDiasCierreFijoAsync();
    Task<List<DateOnly>> ObtenerFechasCierreEventualAsync(DateOnly desde);
    Task<List<HorarioOperacion>> ObtenerHorariosOperacionAsync();

    /// Reservas de una fecha con cliente, estado y mesas, ordenadas por hora (entidades rastreadas).
    Task<List<Reserva>> ObtenerPorFechaAsync(DateOnly fecha);

    /// <summary>HU-OPE-005: reservas de hoy en adelante cuyo nombre o teléfono coincidan con el término.</summary>
    Task<List<Reserva>> BuscarPorNombreOTelefonoAsync(string termino, int limite);

    /// <summary>Verifica si ya existe una reserva con ese código (evita duplicados).</summary>
    Task<bool> ExisteCodigoAsync(string codigo);

    /// <summary>Reservas Confirmadas de una fecha con sus mesas cargadas, para el estado en vivo del canvas (HU-MES-002).</summary>
    Task<List<Reserva>> ObtenerConfirmadasPorFechaAsync(DateOnly fecha);

    /// <summary>Reserva Confirmada y atendida que ocupa esa mesa hoy, para liberarla (HU-OPE-004).</summary>
    Task<Reserva?> ObtenerConfirmadaActivaPorMesaAsync(int idMesa, DateOnly fecha);

    /// <summary>Agrega una nueva reserva al contexto (HU-RES-003).</summary>
    void Agregar(Reserva reserva);

    Task GuardarCambiosAsync();
}