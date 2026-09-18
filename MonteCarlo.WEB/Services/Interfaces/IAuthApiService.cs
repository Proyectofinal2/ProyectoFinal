using MonteCarlo.WEB.Models.Autenticacion;

namespace MonteCarlo.WEB.Services.Interfaces
{
    public interface IAuthApiService
    {
        Task<(bool Success, string Message, LoginApiResponse? Data, int? SegundosBloqueoRestantes)> LoginAsync(string usuario, string contrasena);
    }
}
