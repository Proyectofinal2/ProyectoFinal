using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// Destino tras iniciar sesion (HU-AUT-001, escenario 1).
/// Panel principal del administrador. Se rellena con los módulos OPE y REP.
/// </summary>
[Area("Admin")]
[Route("admin/panel")]
[Authorize]
public class PanelController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
