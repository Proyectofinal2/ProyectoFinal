using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Services.Interfaces;

public interface IMesasService
{
    Task<Result<List<MesaResponse>>> ListarAsync(bool incluirDeshabilitadas);

    Task<Result<MesaResponse>> CrearAsync(CrearMesaRequest request);

    Task<Result<MesaResponse>> ActualizarCapacidadAsync(int idMesa, ActualizarCapacidadRequest request);

    Task<Result<MesaResponse>> ActualizarEstadoAsync(int idMesa, ActualizarEstadoRequest request);

    Task<Result<MesaResponse>> ActualizarPosicionAsync(int idMesa, ActualizarPosicionRequest request);

    Task<Result> EliminarAsync(int idMesa);
}
