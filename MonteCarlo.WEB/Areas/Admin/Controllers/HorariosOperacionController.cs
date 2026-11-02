using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Configuracion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>HU-CFG-005: configuración de horarios regulares de operación.</summary>
[Area("Admin")]
[Route("admin/horarios-operacion")]
[Authorize(Roles = Roles.General)]
public class HorariosOperacionController(IConfiguracionApiService configuracionApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await ConstruirModeloAsync());

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(HorariosOperacionViewModel modelo)
    {
        if (modelo.Dias.Count != HorariosOperacionViewModel.DiasSemana.Count
            || modelo.Dias.Select(d => d.DiaSemana).Distinct().Count() != HorariosOperacionViewModel.DiasSemana.Count)
        {
            ModelState.AddModelError(string.Empty, "La configuracion de horarios esta incompleta.");
        }

        if (!ModelState.IsValid)
        {
            await ActualizarMetadatosAsync(modelo);
            return View("Index", modelo);
        }

        // Los cierres fijos se excluyen del envio normal; la API vuelve a
        // validarlos para no confiar en este valor enviado por el navegador.
        var horariosEditables = modelo.Dias
            .Where(d => !d.EsCierreFijo && d.HoraApertura.HasValue && d.HoraCierre.HasValue)
            .ToList();
        var (success, message, _) = await configuracionApiService.GuardarHorariosOperacionLoteAsync(horariosEditables);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await ActualizarMetadatosAsync(modelo);
            return View("Index", modelo);
        }

        TempData[Alerta.Exito] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("historial")]
    public async Task<IActionResult> Historial(
        string? busqueda, int? diaSemana, int? idUsuario, string? accion,
        DateOnly? desde, DateOnly? hasta, int pagina = 1)
    {
        var paginaSolicitada = Math.Max(pagina, 1);
        var modelo = new HistorialHorariosOperacionViewModel
        {
            Busqueda = busqueda,
            DiaSemana = diaSemana,
            IdUsuario = idUsuario,
            Accion = accion,
            Desde = desde,
            Hasta = hasta
        };

        var historialTask = configuracionApiService.ObtenerHistorialHorariosOperacionAsync(
            busqueda, diaSemana, idUsuario, accion, desde, hasta, paginaSolicitada);
        var usuariosTask = configuracionApiService.ObtenerUsuariosHistorialHorariosOperacionAsync();
        await Task.WhenAll(historialTask, usuariosTask);

        var (success, message, resultado) = await historialTask;
        if (success)
        {
            modelo.Pagina = resultado ?? new();
            if (modelo.Pagina.Items.Count == 0 && modelo.Pagina.Total > 0 && paginaSolicitada > modelo.Pagina.TotalPaginas)
            {
                return RedirectToAction(nameof(Historial), new
                {
                    busqueda, diaSemana, idUsuario, accion, desde, hasta, pagina = modelo.Pagina.TotalPaginas
                });
            }
        }
        else
        {
            modelo.Error = message;
        }

        var (usuariosOk, _, usuarios) = await usuariosTask;
        modelo.Usuarios = usuariosOk ? usuarios ?? [] : [];
        return View(modelo);
    }

    private async Task ActualizarMetadatosAsync(HorariosOperacionViewModel modelo)
    {
        var actual = await ConstruirModeloAsync();
        var porDia = actual.Dias.ToDictionary(d => d.DiaSemana);

        for (var indice = 0; indice < modelo.Dias.Count; indice++)
        {
            var dia = modelo.Dias[indice];
            if (!porDia.TryGetValue(dia.DiaSemana, out var estadoActual))
            {
                continue;
            }

            dia.NombreDia = estadoActual.NombreDia;
            dia.EsCierreFijo = estadoActual.EsCierreFijo;
            ModelState.Remove($"Dias[{indice}].NombreDia");
            ModelState.Remove($"Dias[{indice}].EsCierreFijo");
        }
    }

    private async Task<HorariosOperacionViewModel> ConstruirModeloAsync()
    {
        var horariosTask = configuracionApiService.ObtenerHorariosOperacionAsync();
        var cierresTask = configuracionApiService.ObtenerCierresFijosAsync();
        await Task.WhenAll(horariosTask, cierresTask);

        var horarios = horariosTask.Result;
        var cierres = cierresTask.Result;
        if (!horarios.Success)
            TempData[Alerta.Error] = horarios.Message;
        else if (!cierres.Success)
            TempData[Alerta.Error] = cierres.Message;

        var porDia = (horarios.Data ?? []).ToDictionary(h => h.DiaSemana);
        var diasCerrados = (cierres.Data?.Dias ?? []).ToHashSet();
        return new HorariosOperacionViewModel
        {
            Dias = HorariosOperacionViewModel.DiasSemana.Select(d =>
            {
                porDia.TryGetValue(d.Valor, out var horario);
                return new HorarioOperacionDiaViewModel
                {
                    DiaSemana = d.Valor,
                    NombreDia = d.Nombre,
                    HoraApertura = horario?.HoraApertura,
                    HoraCierre = horario?.HoraCierre,
                    EsCierreFijo = diasCerrados.Contains(d.Valor)
                };
            }).ToList()
        };
    }
}
