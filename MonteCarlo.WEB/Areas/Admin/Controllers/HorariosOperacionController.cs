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
    public async Task<IActionResult> Guardar(GuardarHorarioOperacionViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            var pagina = await ConstruirModeloAsync();
            var dia = pagina.Dias.SingleOrDefault(d => d.DiaSemana == modelo.DiaSemana);
            if (dia is not null)
            {
                dia.HoraApertura = modelo.HoraApertura;
                dia.HoraCierre = modelo.HoraCierre;
            }

            return View("Index", pagina);
        }

        var (success, message, _) = await configuracionApiService.GuardarHorarioOperacionAsync(
            modelo.DiaSemana, modelo.HoraApertura!.Value, modelo.HoraCierre!.Value);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return RedirectToAction(nameof(Index));
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
