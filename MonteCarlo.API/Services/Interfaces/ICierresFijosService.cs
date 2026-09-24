using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface ICierresFijosService
{
    Task<Result<CierresFijosResponse>> ObtenerAsync();

    Task<Result<CierresFijosResponse>> ActualizarAsync(IEnumerable<int> dias, int idUsuario);

    Task<bool> EsCierreFijoAsync(byte diaSemana);
}
