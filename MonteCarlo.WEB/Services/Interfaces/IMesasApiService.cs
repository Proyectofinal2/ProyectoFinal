using MonteCarlo.WEB.Models.Mesas;

namespace MonteCarlo.WEB.Services.Interfaces;

public interface IMesasApiService
{
    Task<(bool Success, string Message, List<MesaApiResponse>? Data)> ObtenerMesasAsync(bool incluirDeshabilitadas);

    Task<(bool Success, string Message, SugerenciaWalkInApiResponse? Data)> ObtenerSugerenciaWalkInAsync(int cantidadPersonas);

    Task<(bool Success, string Message, ReservaWalkInApiResponse? Data)> CrearWalkInAsync(int cantidadPersonas, List<int> idsMesa);

    Task<(bool Success, string Message)> LiberarMesaAsync(int idMesa);

    // ============================================================
    // HU-MES-001/002/003/006 -- forwarding 1:1 a los endpoints de la API
    // (ver MesasService.cs del lado de la API para el detalle de que
    // logica de negocio falta en cada uno; aqui solo se reenvia).
    // ============================================================

    Task<(bool Success, string Message, MesaApiResponse? Data)> CrearMesaAsync(int numeroMesa, int capacidad, int posX, int posY);

    Task<(bool Success, string Message, MesaApiResponse? Data)> ActualizarCapacidadAsync(int idMesa, int capacidad);

    Task<(bool Success, string Message, MesaApiResponse? Data)> ActualizarEstadoAsync(int idMesa, string estado);

    Task<(bool Success, string Message, MesaApiResponse? Data)> ActualizarPosicionAsync(int idMesa, int posX, int posY);

    Task<(bool Success, string Message)> EliminarMesaAsync(int idMesa);
}
