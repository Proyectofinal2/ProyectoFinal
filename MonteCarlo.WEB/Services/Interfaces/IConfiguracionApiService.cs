using MonteCarlo.WEB.Models.Configuracion;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IConfiguracionApiService
{
    /// Pantalla única de Parametros: un solo GET, un solo PUT.
    Task<(bool Success, string Message, ConfiguracionGeneralApiResponse? Data)> ObtenerConfiguracionGeneralAsync();

    /// PATCH simplificado: solo envía los parámetros que cambiaron (ver ParametrosViewModel.ObtenerCambios).
    Task<(bool Success, string Message, ConfiguracionGeneralApiResponse? Data)> ActualizarConfiguracionGeneralAsync(
        IEnumerable<(string Clave, string Valor)> cambios);

    Task<(bool Success, string Message, CierresFijosApiResponse? Data)> ObtenerCierresFijosAsync();

    Task<(bool Success, string Message, CierresFijosApiResponse? Data)> ActualizarCierresFijosAsync(IEnumerable<int> dias);

    Task<(bool Success, string Message, List<HorarioOperacionApiResponse>? Data)> ObtenerHorariosOperacionAsync();

    Task<(bool Success, string Message, HorarioOperacionApiResponse? Data)> GuardarHorarioOperacionAsync(
        int diaSemana, TimeOnly horaApertura, TimeOnly horaCierre);
}
