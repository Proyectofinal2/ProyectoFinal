using MonteCarlo.WEB.Models.Configuracion;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IConfiguracionApiService
{
    Task<(bool Success, string Message, UmbralApiResponse? Data)> ObtenerUmbralAsync();

    Task<(bool Success, string Message, UmbralApiResponse? Data)> ActualizarUmbralAsync(int umbral);

    Task<(bool Success, string Message, CierresFijosApiResponse? Data)> ObtenerCierresFijosAsync();

    Task<(bool Success, string Message, CierresFijosApiResponse? Data)> ActualizarCierresFijosAsync(IEnumerable<int> dias);

    Task<(bool Success, string Message, List<HorarioOperacionApiResponse>? Data)> ObtenerHorariosOperacionAsync();

    Task<(bool Success, string Message, HorarioOperacionApiResponse? Data)> GuardarHorarioOperacionAsync(
        int diaSemana, TimeOnly horaApertura, TimeOnly horaCierre);
}
