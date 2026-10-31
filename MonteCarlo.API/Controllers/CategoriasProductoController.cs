using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>
/// Categorías del menú (HU-CON-001). Cualquier administrador las crea, renombra, reordena y deshabilita; no se eliminan.
/// </summary>
[ApiController]
[Route("api/categorias-producto")]
[Authorize(Roles = "General,Regular")]
public class CategoriasProductoController(ICategoriasProductoService categoriasService) : ControllerBase
{
    /// <summary>Id de la cuenta autenticada (claim del token JWT).</summary>
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lista las categorías en el orden del menú, con el total de productos y cuántos están disponibles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener() =>
        this.Responder(await categoriasService.ObtenerTodasAsync());

    /// <summary>Crea una categoría al final del menú; queda activa.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarCategoriaProductoRequest request) =>
        this.Responder(await categoriasService.CrearAsync(request, IdUsuarioAutenticado));

    /// <summary>Cambia el nombre de la categoría (es lo único que se edita). Se refleja en todos sus productos.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Renombrar(int id, [FromBody] GuardarCategoriaProductoRequest request) =>
        this.Responder(await categoriasService.RenombrarAsync(id, request, IdUsuarioAutenticado));

    /// <summary>Sube la categoría una posición en el menú. Responde con la lista ya reordenada.</summary>
    [HttpPost("{id:int}/subir")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Subir(int id) =>
        this.Responder(await categoriasService.MoverAsync(id, -1, IdUsuarioAutenticado));

    /// <summary>Baja la categoría una posición en el menú. Responde con la lista ya reordenada.</summary>
    [HttpPost("{id:int}/bajar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Bajar(int id) =>
        this.Responder(await categoriasService.MoverAsync(id, 1, IdUsuarioAutenticado));

    /// <summary>Deshabilita la categoría y todos sus productos disponibles. Informa cuántos productos se afectaron.</summary>
    [HttpPost("{id:int}/deshabilitar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deshabilitar(int id) =>
        this.Responder(await categoriasService.DeshabilitarAsync(id, IdUsuarioAutenticado));

    /// <summary>Elimina la categoría solo si no tiene productos (409 si los tiene). Su historial se conserva.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id) =>
        this.Responder(await categoriasService.EliminarAsync(id, IdUsuarioAutenticado));

    /// <summary>Habilita la categoría y todos sus productos, incluso los que se habían deshabilitado por separado.</summary>
    [HttpPost("{id:int}/habilitar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Habilitar(int id) =>
        this.Responder(await categoriasService.HabilitarAsync(id, IdUsuarioAutenticado));
}
