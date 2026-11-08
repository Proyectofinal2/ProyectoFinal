namespace MonteCarlo.WEB.Models.Home;

/// <summary>Modelo de la página principal con información pública de horarios.</summary>
public class HomeIndexViewModel
{
    public bool HorariosDisponibles { get; set; }
    public List<HorarioHomeGrupoViewModel> Horarios { get; set; } = [];
}

/// <summary>Grupo visual de días consecutivos con el mismo estado y horario.</summary>
public class HorarioHomeGrupoViewModel
{
    public string Dias { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public TimeOnly? HoraApertura { get; set; }
    public TimeOnly? HoraCierre { get; set; }

    public bool EsAbierto => Estado == "Abierto";

    public string TextoHorario => EsAbierto
        ? string.Concat(HoraApertura!.Value.ToString("HH:mm"), " – ", HoraCierre!.Value.ToString("HH:mm"))
        : Estado == "Cerrado" ? "Cerrado" : "Horario no disponible";
}
