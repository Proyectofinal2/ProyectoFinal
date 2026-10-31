using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Controllers;

/// <summary>
/// Catalogo de productos (HU-CON-001): registrar, cambiar el precio, deshabilitar y eliminar (solo sin ventas).
/// Lo gestiona cualquier administrador; el historial de cambios es solo del rol "General".
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "General,Regular")]
public class ProductosController(
    IProductosService productosService,
    IHistorialCatalogoService historialService) : ControllerBase
{
    /// <summary>Id de la cuenta autenticada (claim del token JWT).</summary>
    private int IdUsuarioAutenticado =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lista los productos en el orden del menú. Todos los filtros son opcionales.</summary>
    /// <param name="busqueda">Texto contenido en el nombre.</param>
    /// <param name="idCategoria">Solo los de esa categoría.</param>
    /// <param name="disponible">true = disponibles, false = deshabilitados.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? busqueda, [FromQuery] int? idCategoria, [FromQuery] bool? disponible) =>
        this.Responder(await productosService.ObtenerTodosAsync(busqueda, idCategoria, disponible));

    /// <summary>Obtiene un producto (para editarlo).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id) =>
        this.Responder(await productosService.ObtenerPorIdAsync(id));

    /// <summary>HU-CON-001, escenario 1: registra un producto; queda disponible.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] GuardarProductoRequest request) =>
        this.Responder(await productosService.CrearAsync(request, IdUsuarioAutenticado));

    /// <summary>HU-CON-001, escenario 2: actualiza nombre, precio y/o categoría. Las ventas ya registradas conservan su precio.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarProductoRequest request) =>
        this.Responder(await productosService.ActualizarAsync(id, request, IdUsuarioAutenticado));

    /// <summary>HU-CON-001, escenario 3: deshabilita el producto. No se elimina; el historial de ventas se conserva.</summary>
    [HttpPost("{id:int}/deshabilitar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deshabilitar(int id) =>
        this.Responder(await productosService.DeshabilitarAsync(id, IdUsuarioAutenticado));

    /// <summary>Elimina el producto solo si no tiene ventas asociadas (409 si las tiene). Su historial se conserva.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id) =>
        this.Responder(await productosService.EliminarAsync(id, IdUsuarioAutenticado));

    /// <summary>
    /// Habilita el producto. Si su categoría está deshabilitada responde 409 con los datos de la categoría, salvo que
    /// se envíe <c>habilitarCategoria=true</c>: entonces se habilitan el producto y la categoría.
    /// </summary>
    [HttpPost("{id:int}/habilitar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Habilitar(int id, [FromQuery] bool habilitarCategoria = false) =>
        this.Responder(await productosService.HabilitarAsync(id, habilitarCategoria, IdUsuarioAutenticado));

    /// <summary>Últimos cambios de un producto. Solo el rol "General".</summary>
    [HttpGet("{id:int}/historial")]
    [Authorize(Roles = "General")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHistorial(int id) =>
        this.Responder(await historialService.ObtenerPorProductoAsync(id));
}
