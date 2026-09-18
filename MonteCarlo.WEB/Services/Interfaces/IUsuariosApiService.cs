using MonteCarlo.WEB.Models.Autenticacion;

namespace MonteCarlo.WEB.Services.Interfaces
{
    public interface IUsuariosApiService
    {
        Task<(bool Success, string Message, UserApiResponse? Data)> CrearUsuarioAsync(NuevaCuentaViewModel modelo);
        Task<(bool Success, string Message, List<UserApiResponse>? Data)> ObtenerTodosAsync(string? busqueda, FiltroEstadoCuenta estado);
        Task<(bool Success, string Message, UserApiResponse? Data)> DesactivarAsync(int id);
        Task<(bool Success, string Message, UserApiResponse? Data)> ReactivarAsync(int id);
    }
}
