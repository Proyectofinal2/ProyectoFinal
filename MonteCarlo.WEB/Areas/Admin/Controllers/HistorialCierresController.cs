using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// Historial de cambios en cierres fijos y eventuales.
[Area("Admin")]
[Route("admin/historial-cierres")]
[Authorize(Roles = Roles.General)]
public class HistorialCierresController(IConfiguracionApiService configuracionApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? tipo, string? accion, DateOnly? desde, DateOnly? hasta, int pagina = 1)
    {
        var modelo = new HistorialCierresViewModel { Tipo = tipo, Accion = accion, Desde = desde, Hasta = hasta };

        var (success, message, data) = await configuracionApiService.ObtenerHistorialCierresAsync(
            tipo, accion, desde, hasta);

        if (success)
        {
            // La API devuelve el historial filtrado (maximo 200); se pagina aqui.
            var todos = data ?? [];
            modelo.Total = todos.Count;
            modelo.TotalPaginas = Math.Max(1, (int)Math.Ceiling(todos.Count / (double)HistorialCierresViewModel.TamanoPagina));
            modelo.Pagina = Math.Clamp(pagina, 1, modelo.TotalPaginas);
            modelo.Items = todos
                .Skip((modelo.Pagina - 1) * HistorialCierresViewModel.TamanoPagina)
                .Take(HistorialCierresViewModel.TamanoPagina)
                .ToList();
        }
        else
        {
            TempData[Alerta.Error] = message;
        }

        return View(modelo);
    }
}
