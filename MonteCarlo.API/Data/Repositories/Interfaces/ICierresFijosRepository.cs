using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface ICierresFijosRepository
{
	Task<List<CierreFijo>> ObtenerTodosAsync();
	/// Aplica altas, bajas y su registro de auditoría en un solo SaveChanges
	/// (una transacción): o se guarda todo o nada.

	Task GuardarCambiosAsync(
		IEnumerable<CierreFijo> agregar,
		IEnumerable<CierreFijo> eliminar,
		IEnumerable<HistorialCierre> historial);
}
