using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/metodos-pago")]
[Authorize(Roles = "General,Regular")]
public class MetodosPagoController(IConfiguracionApiService configuracionApiService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var (success, message, metodos) = await configuracionApiService.ObtenerMetodosPagoAdminAsync();
        if (!success) TempData["Alerta.Error"] = message;

        return View(new MetodosPagoViewModel { Metodos = metodos ?? [] });
    }

    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre)
    {
        var (success, message) = await configuracionApiService.CrearMetodoPagoAsync(nombre);
        TempData[success ? "Alerta.Exito" : "Alerta.Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cambiar-estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var (success, message) = await configuracionApiService.CambiarEstadoMetodoPagoAsync(id, activo);
        TempData[success ? "Alerta.Exito" : "Alerta.Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("historial")]
    public async Task<IActionResult> Historial(
        string? busqueda, int? idMetodoPago, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina = 1)
    {
        var paginaSolicitada = Math.Max(pagina, 1);
        var modelo = new HistorialMetodosPagoViewModel
        {
            Busqueda = busqueda,
            IdMetodoPago = idMetodoPago,
            IdUsuario = idUsuario,
            Accion = accion,
            Desde = desde,
            Hasta = hasta
        };

        var historialTask = configuracionApiService.ObtenerHistorialMetodosPagoAsync(
            busqueda, idMetodoPago, idUsuario, accion, desde, hasta, paginaSolicitada);
        var usuariosTask = configuracionApiService.ObtenerUsuariosHistorialMetodosPagoAsync();
        var metodosTask = configuracionApiService.ObtenerMetodosPagoAdminAsync();
        await Task.WhenAll(historialTask, usuariosTask, metodosTask);

        var (success, message, resultado) = await historialTask;
        if (success)
        {
            modelo.Pagina = resultado ?? new();
            if (modelo.Pagina.Items.Count == 0 && modelo.Pagina.Total > 0 && paginaSolicitada > modelo.Pagina.TotalPaginas)
            {
                return RedirectToAction(nameof(Historial), new
                {
                    busqueda, idMetodoPago, idUsuario, accion, desde, hasta, pagina = modelo.Pagina.TotalPaginas
                });
            }
        }
        else
        {
            modelo.Error = message;
        }

        var (usuariosOk, _, usuarios) = await usuariosTask;
        modelo.Usuarios = usuariosOk ? usuarios ?? [] : [];
        var (metodosOk, _, metodos) = await metodosTask;
        modelo.Metodos = metodosOk ? metodos ?? [] : [];
        return View(modelo);
    }
}
