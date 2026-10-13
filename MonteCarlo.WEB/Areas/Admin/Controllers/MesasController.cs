using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Mesas;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// HU-MES-002 / HU-OPE-004: canvas unico de mesas (ver la vista Index.cshtml para
/// el detalle de ambos modos). Las acciones de abajo son JSON puro -- las llama el
/// fetch() del canvas, no formularios -- y actuan como proxy hacia la API, igual
/// que el resto de los *ApiService del proyecto (Bearer token via AuthTokenHandler).
[Area("Admin")]
[Route("admin/mesas")]
[Authorize(Roles = "General,Regular")]
public class MesasController(IMesasApiService mesasApiService) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new MesasIndexViewModel
    {
        EsAdministradorGeneral = User.IsInRole(Roles.General)
    });

    [HttpGet("listado")]
    public async Task<IActionResult> Listado(bool incluirDeshabilitadas = false)
    {
        var (success, message, data) = await mesasApiService.ObtenerMesasAsync(incluirDeshabilitadas);
        return Json(new { success, message, data });
    }

    [HttpGet("sugerencia-walkin")]
    public async Task<IActionResult> SugerenciaWalkIn(int cantidadPersonas)
    {
        var (success, message, data) = await mesasApiService.ObtenerSugerenciaWalkInAsync(cantidadPersonas);
        return Json(new { success, message, data });
    }

    [HttpPost("walkin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearWalkIn([FromBody] CrearWalkInBody body)
    {
        var (success, message, data) = await mesasApiService.CrearWalkInAsync(body.CantidadPersonas, body.IdsMesa);
        return Json(new { success, message, data });
    }

    [HttpPut("{id:int}/liberar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Liberar(int id)
    {
        var (success, message) = await mesasApiService.LiberarMesaAsync(id);
        return Json(new { success, message });
    }

    // ============================================================
    // HU-MES-001/002/003/006 -- companero de equipo. El canvas ya llama
    // estas acciones (modo "Editar salon"); la logica de negocio real
    // vive del lado de la API (MesasService.cs).
    // ============================================================

    [Authorize(Roles = Roles.General)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([FromBody] CrearMesaBody body)
    {
        var (success, message, data) = await mesasApiService.CrearMesaAsync(body.NumeroMesa, body.Capacidad, body.PosX, body.PosY);
        return Json(new { success, message, data });
    }

    [Authorize(Roles = Roles.General)]
    [HttpPut("{id:int}/capacidad")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarCapacidad(int id, [FromBody] ActualizarCapacidadBody body)
    {
        var (success, message, data) = await mesasApiService.ActualizarCapacidadAsync(id, body.Capacidad);
        return Json(new { success, message, data });
    }

    [Authorize(Roles = Roles.General)]
    [HttpPut("{id:int}/estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoBody body)
    {
        var (success, message, data) = await mesasApiService.ActualizarEstadoAsync(id, body.Estado);
        return Json(new { success, message, data });
    }

    [Authorize(Roles = Roles.General)]
    [HttpPut("{id:int}/posicion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarPosicion(int id, [FromBody] ActualizarPosicionBody body)
    {
        var (success, message, data) = await mesasApiService.ActualizarPosicionAsync(id, body.PosX, body.PosY);
        return Json(new { success, message, data });
    }

    [Authorize(Roles = Roles.General)]
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var (success, message) = await mesasApiService.EliminarMesaAsync(id);
        return Json(new { success, message });
    }

    public record CrearWalkInBody(int CantidadPersonas, List<int> IdsMesa);
    public record CrearMesaBody(int NumeroMesa, int Capacidad, int PosX, int PosY);
    public record ActualizarCapacidadBody(int Capacidad);
    public record ActualizarEstadoBody(string Estado);
    public record ActualizarPosicionBody(int PosX, int PosY);
}
