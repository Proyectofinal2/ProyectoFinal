using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface ICierresEventualesRepository
{
    /// Cierres activos con fecha igual o posterior a la indicada, con su usuario.
    Task<List<CierreEventual>> ObtenerDesdeAsync(DateOnly desde);

    Task<CierreEventual?> ObtenerPorIdAsync(int id);

    Task<bool> ExistePorFechaAsync(DateOnly fecha);

    void Agregar(CierreEventual cierre);

    void AgregarHistorial(HistorialCierre historial);

    Task GuardarCambiosAsync();
}
