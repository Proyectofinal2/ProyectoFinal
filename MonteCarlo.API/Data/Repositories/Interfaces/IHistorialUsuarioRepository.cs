using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

/// <summary>Consulta del historial de cuentas (HU-AUT-007). Las filas se escriben desde <see cref="IUsuariosRepository"/>.</summary>
public interface IHistorialUsuarioRepository
{
    Task<(List<HistorialUsuario> Items, int Total)> BuscarAsync(
        string? busqueda, int? idUsuarioAfectado, int? idUsuario, string? accion, int pagina, int tamano);

    /// <summary>Administradores que hicieron algún cambio (para el filtro «Realizado por»).</summary>
    Task<List<Usuario>> ObtenerResponsablesAsync();
}
