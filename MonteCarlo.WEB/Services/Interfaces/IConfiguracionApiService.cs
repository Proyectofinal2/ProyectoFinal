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

    Task<(bool Success, string Message, List<CierreEventualApiResponse>? Data)> ObtenerCierresEventualesAsync();

    Task<(bool Success, string Message, CierreEventualApiResponse? Data)> RegistrarCierreEventualAsync(
        DateOnly fecha, string motivo, bool cancelarReservas);

    Task<(bool Success, string Message, CierreEventualApiResponse? Data)> EliminarCierreEventualAsync(int id);

    Task<(bool Success, string Message, List<HistorialCierreApiResponse>? Data)> ObtenerHistorialCierresAsync(
        string? tipo, string? accion, DateOnly? desde, DateOnly? hasta);

    Task<(bool Success, string Message, List<HorarioOperacionApiResponse>? Data)> ObtenerHorariosOperacionAsync();

    Task<(bool Success, string Message, HorarioOperacionApiResponse? Data)> GuardarHorarioOperacionAsync(
        int diaSemana, TimeOnly horaApertura, TimeOnly horaCierre);

    Task<(bool Success, string Message, List<HorarioOperacionApiResponse>? Data)> GuardarHorariosOperacionLoteAsync(
        IEnumerable<HorarioOperacionDiaViewModel> horarios);

    Task<(bool Success, string Message, PaginaHistorialHorarioOperacionApiResponse? Data)> ObtenerHistorialHorariosOperacionAsync(
        string? busqueda, int? diaSemana, int? idUsuario, string? accion, DateOnly? desde, DateOnly? hasta, int pagina);

    Task<(bool Success, string Message, List<UsuarioHistorialHorarioOperacionApiResponse>? Data)> ObtenerUsuariosHistorialHorariosOperacionAsync();

    Task<(bool Success, string Message, List<MetodoPagoAdminApiResponse>? Data)> ObtenerMetodosPagoAdminAsync();
    Task<(bool Success, string Message)> CrearMetodoPagoAsync(string nombre);
    Task<(bool Success, string Message)> CambiarEstadoMetodoPagoAsync(int id, bool activo);

    Task<(bool Success, string Message, PaginaHistorialMetodoPagoApiResponse? Data)> ObtenerHistorialMetodosPagoAsync(
        string? busqueda, int? idMetodoPago, int? idUsuario, string? accion, DateOnly? desde, DateOnly? hasta, int pagina);

    Task<(bool Success, string Message, List<UsuarioHistorialMetodoPagoApiResponse>? Data)> ObtenerUsuariosHistorialMetodosPagoAsync();
}
