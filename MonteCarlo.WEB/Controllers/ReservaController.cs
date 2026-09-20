using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models.Reservas;

namespace MonteCarlo.WEB.Controllers;


public class ReservaController : Controller
{
    // HU-RES-002 Muestra el calendario
 
    [HttpGet]
    public IActionResult Create()
    {
        return View(new NuevaReservaDisponibilidadViewModel());
    }
}
