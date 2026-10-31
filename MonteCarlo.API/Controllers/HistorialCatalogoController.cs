using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>Historial de cambios de productos y categorías (HU-CON-001). Solo el rol "General".</summary>
[ApiController]
[Route("api/historial-catalogo")]
[Authorize(Roles = "General")]
public class HistorialCatalogoController(IHistorialCatalogoService historialService) : ControllerBase
{
    /// <summary>
    /// Historial general, del más reciente al más antiguo, paginado (máximo 10 por página).
    /// </summary>
    /// <param name="busqueda">Texto contenido en el nombre del producto o de la categoría.</param>
    /// <param name="tipo">"Producto" o "Categoria".</param>
    /// <param name="idUsuario">Solo los cambios de ese administrador.</param>
    /// <param name="accion">Creado, Editado, Reordenado, Habilitado o Deshabilitado.</param>
    /// <param name="pagina">Página a mostrar (desde 1).</param>
    /// <param name="tamano">Filas por página; nunca más de 10.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? busqueda, [FromQuery] string? tipo, [FromQuery] int? idUsuario,
        [FromQuery] string? accion, [FromQuery] int pagina = 1, [FromQuery] int tamano = IHistorialCatalogoService.TamanoPagina) =>
        this.Responder(await historialService.ObtenerAsync(busqueda, tipo, idUsuario, accion, pagina, tamano));

    /// <summary>Administradores que aparecen en el historial (para el filtro por usuario).</summary>
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerUsuarios() =>
        this.Responder(await historialService.ObtenerUsuariosAsync());
}
