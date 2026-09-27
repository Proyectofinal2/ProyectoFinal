using MonteCarlo.WEB.Models.Autenticacion;

namespace MonteCarlo.WEB.Services.Interfaces
{
    public interface IAuthApiService
    {
        Task<(bool Success, string Message, LoginApiResponse? Data, int? SegundosBloqueoRestantes)> LoginAsync(string usuario, string contrasena);

        Task<(bool Success, string Message)> RecuperarContrasenaAsync(string usuarioOCorreo);

        Task<(bool Success, string Message, bool TokenInvalido)> RestablecerContrasenaAsync(string token, string nuevaContrasena);

        Task<bool> ValidarTokenRecuperacionAsync(string token);
    }
}
