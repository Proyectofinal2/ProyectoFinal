using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Panel;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// Destino tras iniciar sesion (HU-AUT-001, escenario 1).
/// Panel principal del administrador. Se rellena con los módulos OPE y REP.
/// </summary>
[Area("Admin")]
[Route("admin/panel")]
[Authorize(Roles = "General,Regular")]
public class PanelController(
    IMesasApiService mesasApiService,
    IReservasApiService reservasApiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(FiltrosDashboardViewModel filtros)
    {
        var (fechaInicio, fechaFin) = filtros.ResolverRango(DateOnly.FromDateTime(DateTime.Now));
        var resumenMesasTask = mesasApiService.ObtenerResumenAsync();
        var estadosTask = reservasApiService.ObtenerEstadosReporteAsync();

        var reporteTask = reservasApiService.ObtenerReporteAsync(
            fechaInicio, fechaFin, filtros.EstadoReserva);

        await Task.WhenAll(resumenMesasTask, estadosTask, reporteTask);
        var (successMesasReporte, messageMesasReporte, dataMesasReporte) = await resumenMesasTask;
        var (successEstadosReporte, messageEstadosReporte, dataEstadosReporte) = await estadosTask;
        var (successReporte, messageReporte, dataReporte) = await reporteTask;

        return View(new PanelIndexViewModel
        {
            Filtros = filtros,
            ResumenMesas = successMesasReporte ? dataMesasReporte : null,
            FechaActualizacion = successMesasReporte ? DateTime.Now : null,
            ErrorResumen = successMesasReporte ? null : messageMesasReporte,
            EstadosReserva = successEstadosReporte ? dataEstadosReporte ?? [] : [],
            ReporteReservas = successReporte ? dataReporte : null,
            ErrorReporte = successReporte ? null : messageReporte
        });
    }
}
