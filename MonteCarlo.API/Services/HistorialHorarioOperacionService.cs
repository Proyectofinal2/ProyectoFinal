using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class HistorialHorarioOperacionService(
    IHistorialHorarioOperacionRepository historialRepository) : IHistorialHorarioOperacionService
{
    private static readonly IReadOnlyDictionary<string, byte> DiasPorNombre =
        new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            ["domingo"] = 0,
            ["lunes"] = 1,
            ["martes"] = 2,
            ["miercoles"] = 3,
            ["miércoles"] = 3,
            ["jueves"] = 4,
            ["viernes"] = 5,
            ["sabado"] = 6,
            ["sábado"] = 6
        };

    private static readonly string[] NombresDias =
        ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    public async Task<Result<PaginaResponse<HistorialHorarioOperacionResponse>>> ObtenerAsync(
        string? busqueda, int? diaSemana, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina, int tamano)
    {
        if (diaSemana is < 0 or > 6)
            return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.BadRequest(
                "El día debe estar entre 0 (domingo) y 6 (sábado).");

        if (accion is not null && !HistorialHorarioOperacionAcciones.Todas.Contains(accion))
            return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.BadRequest(
                $"La acción debe ser una de: {string.Join(", ", HistorialHorarioOperacionAcciones.Todas)}.");

        if (desde > hasta)
            return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.BadRequest(
                "La fecha inicial no puede ser posterior a la final.");

        if (pagina < 1)
            return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.BadRequest("La página debe ser 1 o mayor.");

        byte? diaPorBusqueda = null;
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            if (!DiasPorNombre.TryGetValue(busqueda.Trim(), out var diaBuscado))
            {
                return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.Ok(
                    PaginaVacia(pagina, Math.Clamp(tamano, 1, IHistorialHorarioOperacionService.TamanoPagina)),
                    "Historial obtenido exitosamente.");
            }

            diaPorBusqueda = diaBuscado;
        }

        if (diaSemana.HasValue && diaPorBusqueda.HasValue && diaSemana.Value != diaPorBusqueda.Value)
        {
            return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.Ok(
                PaginaVacia(pagina, Math.Clamp(tamano, 1, IHistorialHorarioOperacionService.TamanoPagina)),
                "Historial obtenido exitosamente.");
        }

        tamano = Math.Clamp(tamano, 1, IHistorialHorarioOperacionService.TamanoPagina);
        var diaFiltro = diaSemana.HasValue ? (byte?)diaSemana.Value : diaPorBusqueda;
        var (items, total) = await historialRepository.BuscarAsync(
            diaFiltro, idUsuario, accion, desde, hasta, pagina, tamano);

        return Result<PaginaResponse<HistorialHorarioOperacionResponse>>.Ok(
            new PaginaResponse<HistorialHorarioOperacionResponse>
            {
                Items = items.Select(Mapear).ToList(),
                Pagina = pagina,
                TamanoPagina = tamano,
                Total = total,
                TotalPaginas = (int)Math.Ceiling(total / (double)tamano)
            },
            "Historial obtenido exitosamente.");
    }

    public async Task<Result<List<UsuarioHistorialResponse>>> ObtenerUsuariosAsync()
    {
        var usuarios = await historialRepository.ObtenerUsuariosAsync();
        return Result<List<UsuarioHistorialResponse>>.Ok(
            usuarios.Select(u => new UsuarioHistorialResponse
            {
                IdUsuario = u.IdUsuario,
                NombreCompleto = u.NombreCompleto
            }).ToList(),
            "Usuarios obtenidos exitosamente.");
    }

    private static PaginaResponse<HistorialHorarioOperacionResponse> PaginaVacia(int pagina, int tamano) => new()
    {
        Pagina = pagina,
        TamanoPagina = tamano,
        Total = 0,
        TotalPaginas = 0
    };

    private static HistorialHorarioOperacionResponse Mapear(HistorialHorarioOperacion historial) => new()
    {
        IdHistorialHorarioOperacion = historial.IdHistorialHorarioOperacion,
        IdOperacion = historial.IdOperacion,
        IdHorarioOperacion = historial.IdHorarioOperacion,
        DiaSemana = historial.DiaSemana,
        NombreDia = NombresDias[historial.DiaSemana],
        Accion = historial.Accion,
        HoraAperturaAnterior = historial.HoraAperturaAnterior,
        HoraAperturaNueva = historial.HoraAperturaNueva,
        HoraCierreAnterior = historial.HoraCierreAnterior,
        HoraCierreNuevo = historial.HoraCierreNuevo,
        ActivoAnterior = historial.ActivoAnterior,
        ActivoNuevo = historial.ActivoNuevo,
        IdUsuario = historial.IdUsuario,
        Usuario = historial.Usuario.NombreCompleto,
        RolUsuario = historial.Usuario.Rol,
        Fecha = historial.Fecha
    };
}
