namespace MonteCarlo.WEB.Models.InformacionPublica;

/// <summary>Respuesta pública de GET api/informacion-publica/horarios.</summary>
public class HorariosPublicosApiResponse
{
    public List<HorarioPublicoDiaApiResponse> Dias { get; set; } = [];
}

public class HorarioPublicoDiaApiResponse
{
    public int DiaSemana { get; set; }
    public string Estado { get; set; } = string.Empty;
    public TimeOnly? HoraApertura { get; set; }
    public TimeOnly? HoraCierre { get; set; }
}
