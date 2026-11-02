using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Data.Repositories.Interfaces;

public interface IHistorialHorarioOperacionRepository
{
    /// <summary>Solo agrega al contexto; se persiste junto con el horario que lo origina.</summary>
    void Agregar(HistorialHorarioOperacion entrada);

    Task<(List<HistorialHorarioOperacion> Items, int Total)> BuscarAsync(
        byte? diaSemana, int? idUsuario, string? accion, DateOnly? desde, DateOnly? hasta,
        int pagina, int tamano);

    /// <summary>Administradores que aparecen en el historial, para el filtro de consulta.</summary>
    Task<List<Usuario>> ObtenerUsuariosAsync();
}
