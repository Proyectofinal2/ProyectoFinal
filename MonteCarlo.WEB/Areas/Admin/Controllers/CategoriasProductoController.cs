using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Catalogo;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-CON-001: categorias del menu. Cualquier administrador las crea, renombra, reordena y deshabilita; no se eliminan.
/// El nombre se edita en la misma fila (?editar=id) y la fila nueva aparece al final (?nueva=true): sin JavaScript.
/// </summary>
[Area("Admin")]
[Route("admin/productos/categorias")]
[Authorize(Roles = "General,Regular")]
public class CategoriasProductoController(ICatalogoApiService catalogoApiService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? editar, bool nueva = false) =>
        View(await ConstruirIndexAsync(editar, nueva, null, null));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string? nombre)
    {
        var (success, message, _) = await catalogoApiService.CrearCategoriaAsync(nombre ?? "");
        if (success)
        {
            TempData[Alerta.Exito] = message;
            return RedirectToAction(nameof(Index));
        }

        return View("Index", await ConstruirIndexAsync(null, true, nombre, message));
    }

    [HttpPost("{id:int}/renombrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renombrar(int id, string? nombre)
    {
        var (success, message, _) = await catalogoApiService.RenombrarCategoriaAsync(id, nombre ?? "");
        if (success)
        {
            TempData[Alerta.Exito] = message;
            return RedirectToAction(nameof(Index));
        }

        return View("Index", await ConstruirIndexAsync(id, false, nombre, message));
    }

    [HttpPost("{id:int}/subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subir(int id)
    {
        var (success, message) = await catalogoApiService.SubirCategoriaAsync(id);
        if (!success)
            TempData[Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/bajar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bajar(int id)
    {
        var (success, message) = await catalogoApiService.BajarCategoriaAsync(id);
        if (!success)
            TempData[Alerta.Error] = message;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deshabilita la categoria y todos sus productos; el dialogo de confirmacion ya mostro cuantos son.</summary>
    [HttpPost("{id:int}/deshabilitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deshabilitar(int id)
    {
        var (success, message, _) = await catalogoApiService.DeshabilitarCategoriaAsync(id);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Elimina la categoria solo si no tiene productos (la API lo valida); su historial se conserva.</summary>
    [HttpPost("{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var (success, message) = await catalogoApiService.EliminarCategoriaAsync(id);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Habilita la categoria y todos sus productos.</summary>
    [HttpPost("{id:int}/habilitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Habilitar(int id)
    {
        var (success, message, _) = await catalogoApiService.HabilitarCategoriaAsync(id);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<CategoriasIndexViewModel> ConstruirIndexAsync(int? editar, bool nueva, string? nombre, string? error)
    {
        var modelo = new CategoriasIndexViewModel
        {
            EditarId = editar,
            Nueva = nueva,
            NombreEdicion = nombre,
            ErrorEdicion = error
        };

        var (success, message, categorias) = await catalogoApiService.ObtenerCategoriasAsync();
        if (success)
        {
            modelo.Categorias = categorias ?? [];
            // Al editar por primera vez, la caja arranca con el nombre actual.
            if (editar.HasValue && nombre is null)
                modelo.NombreEdicion = modelo.Categorias.FirstOrDefault(c => c.IdCategoria == editar.Value)?.Nombre;
        }
        else
        {
            TempData[Alerta.Error] = message;
        }

        return modelo;
    }
}
