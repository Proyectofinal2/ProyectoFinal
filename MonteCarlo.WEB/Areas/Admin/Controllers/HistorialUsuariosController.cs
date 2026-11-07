using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-AUT-007: historial de cambios de cuentas de administrador (quien creo, edito, habilito o deshabilito
/// cada cuenta), paginado de 10 en 10. Solo el Administrador General; la API lo exige tambien.
/// </summary>
[Area("Admin")]
[Route("admin/usuarios/historial")]
[Authorize(Roles = Roles.General)]
public class HistorialUsuariosController(IUsuariosApiService usuariosApiService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, int? idUsuario, string? accion, int pagina = 1)
    {
        var modelo = new HistorialUsuariosViewModel { Busqueda = busqueda, IdUsuario = idUsuario, Accion = accion };

        var (success, message, resultado) = await usuariosApiService.ObtenerHistorialAsync(
            busqueda, idUsuario, accion, Math.Max(pagina, 1));

        if (success)
        {
            modelo.Pagina = resultado ?? new();

            // Si se pidio una pagina que ya no existe (por ejemplo, tras filtrar), se muestra la ultima.
            if (modelo.Pagina.Items.Count == 0 && modelo.Pagina.Total > 0 && pagina > modelo.Pagina.TotalPaginas)
            {
                return RedirectToAction(nameof(Index), new
                {
                    busqueda, idUsuario, accion, pagina = modelo.Pagina.TotalPaginas
                });
            }
        }
        else
        {
            TempData[Alerta.Error] = message;
        }

        var (okResponsables, _, responsables) = await usuariosApiService.ObtenerResponsablesHistorialAsync();
        modelo.Responsables = okResponsables ? responsables ?? [] : [];

        return View(modelo);
    }
}
