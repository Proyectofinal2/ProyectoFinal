using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Home;
using MonteCarlo.WEB.Models.InformacionPublica;
using MonteCarlo.WEB.Services.Interfaces;
using System.Diagnostics;

namespace MonteCarlo.WEB.Controllers
{
    public class HomeController(
        IInformacionPublicaApiService informacionPublicaApiService,
        ILogger<HomeController> logger) : Controller
    {
        private static readonly IReadOnlyList<(int Valor, string Nombre)> DiasSemana =
        [
            (1, "Lunes"), (2, "Martes"), (3, "Miércoles"), (4, "Jueves"),
            (5, "Viernes"), (6, "Sábado"), (0, "Domingo")
        ];

        public async Task<IActionResult> Index()
        {
            var (success, message, data) = await informacionPublicaApiService.ObtenerHorariosAsync();
            if (!success || data is null || !EsRespuestaValida(data))
            {
                logger.LogWarning("No se pudieron mostrar los horarios públicos en Home: {Message}", message);
                return View(new HomeIndexViewModel());
            }

            return View(new HomeIndexViewModel
            {
                HorariosDisponibles = true,
                Horarios = AgruparDias(data.Dias)
            });
        }

        private static bool EsRespuestaValida(HorariosPublicosApiResponse respuesta) =>
            respuesta.Dias.Count > 0
            && respuesta.Dias.All(d => d.DiaSemana is >= 0 and <= 6)
            && respuesta.Dias.GroupBy(d => d.DiaSemana).All(g => g.Count() == 1)
            && respuesta.Dias.All(EstadoYHorasSonValidos);

        private static bool EstadoYHorasSonValidos(HorarioPublicoDiaApiResponse horario) =>
            horario.Estado switch
            {
                "Abierto" => horario.HoraApertura.HasValue
                    && horario.HoraCierre.HasValue
                    && horario.HoraCierre > horario.HoraApertura,
                "Cerrado" or "SinHorario" => true,
                _ => false
            };

        private static List<HorarioHomeGrupoViewModel> AgruparDias(IEnumerable<HorarioPublicoDiaApiResponse> diasApi)
        {
            var porDia = diasApi.ToDictionary(d => d.DiaSemana);
            var dias = DiasSemana.Select(d =>
            {
                if (!porDia.TryGetValue(d.Valor, out var horario))
                {
                    return new HorarioHomeGrupoViewModel { Dias = d.Nombre, Estado = "SinHorario" };
                }

                return new HorarioHomeGrupoViewModel
                {
                    Dias = d.Nombre,
                    Estado = horario.Estado,
                    HoraApertura = horario.Estado == "Abierto" ? horario.HoraApertura : null,
                    HoraCierre = horario.Estado == "Abierto" ? horario.HoraCierre : null
                };
            }).ToList();

            var grupos = new List<HorarioHomeGrupoViewModel>();
            foreach (var dia in dias)
            {
                var anterior = grupos.LastOrDefault();
                if (anterior is not null && CompartenHorario(anterior, dia))
                {
                    anterior.Dias += " – " + dia.Dias;
                    continue;
                }

                grupos.Add(dia);
            }

            return grupos;
        }

        private static bool CompartenHorario(HorarioHomeGrupoViewModel primero, HorarioHomeGrupoViewModel segundo) =>
            primero.Estado == segundo.Estado
            && primero.HoraApertura == segundo.HoraApertura
            && primero.HoraCierre == segundo.HoraCierre;

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
