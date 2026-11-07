using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Catalogo;

namespace MonteCarlo.WEB.Services.Interfaces
{
    public interface IUsuariosApiService
    {
        Task<(bool Success, string Message, UserApiResponse? Data)> CrearUsuarioAsync(NuevaCuentaViewModel modelo);
        Task<(bool Success, string Message, List<UserApiResponse>? Data)> ObtenerTodosAsync(string? busqueda, FiltroEstadoCuenta estado);
        Task<(bool Success, string Message, UserApiResponse? Data)> ObtenerPorIdAsync(int id);
        Task<(bool Success, string Message, UserApiResponse? Data)> ActualizarAsync(int id, EditarCuentaViewModel modelo);
        Task<(bool Success, string Message, UserApiResponse? Data)> DesactivarAsync(int id, string? motivo = null);
        Task<(bool Success, string Message, UserApiResponse? Data)> ReactivarAsync(int id, string? motivo = null);
        Task<(bool Success, string Message, PaginaApiResponse<HistorialUsuarioApiResponse>? Data)> ObtenerHistorialAsync(
            string? busqueda, int? idUsuario, string? accion, int pagina);
        Task<(bool Success, string Message, List<UsuarioHistorialApiResponse>? Data)> ObtenerResponsablesHistorialAsync();
    }
}
