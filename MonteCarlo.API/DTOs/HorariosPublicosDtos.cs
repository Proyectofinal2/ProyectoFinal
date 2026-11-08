namespace MonteCarlo.API.DTOs;

/// <summary>Horario semanal que se puede mostrar en la página pública.</summary>
public class HorariosPublicosResponse
{
    public List<HorarioPublicoDiaResponse> Dias { get; set; } = [];
}

/// <summary>Estado público de un día de operación (0=Domingo ... 6=Sábado).</summary>
public class HorarioPublicoDiaResponse
{
    public int DiaSemana { get; set; }
    public string Estado { get; set; } = string.Empty;
    public TimeOnly? HoraApertura { get; set; }
    public TimeOnly? HoraCierre { get; set; }
}
