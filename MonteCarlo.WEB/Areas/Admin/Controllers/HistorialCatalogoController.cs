using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Catalogo;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-CON-001: historial de cambios de productos y categorias, paginado de 10 en 10.
/// Solo el Administrador General; la API lo exige tambien.
/// </summary>
[Area("Admin")]
[Route("admin/productos/historial")]
[Authorize(Roles = Roles.General)]
public class HistorialCatalogoController(ICatalogoApiService catalogoApiService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, string? tipo, int? idUsuario, string? accion, int pagina = 1)
    {
        var modelo = new HistorialCatalogoViewModel { Busqueda = busqueda, Tipo = tipo, IdUsuario = idUsuario, Accion = accion };

        var (success, message, resultado) = await catalogoApiService.ObtenerHistorialAsync(
            busqueda, tipo, idUsuario, accion, Math.Max(pagina, 1));

        if (success)
        {
            modelo.Pagina = resultado ?? new();

            // Si se pidio una pagina que ya no existe (por ejemplo, tras filtrar), se muestra la ultima.
            if (modelo.Pagina.Items.Count == 0 && modelo.Pagina.Total > 0 && pagina > modelo.Pagina.TotalPaginas)
            {
                return RedirectToAction(nameof(Index), new
                {
                    busqueda, tipo, idUsuario, accion, pagina = modelo.Pagina.TotalPaginas
                });
            }
        }
        else
        {
            TempData[Alerta.Error] = message;
        }

        var (okUsuarios, _, usuarios) = await catalogoApiService.ObtenerUsuariosHistorialAsync();
        modelo.Usuarios = okUsuarios ? usuarios ?? [] : [];

        return View(modelo);
    }
}
