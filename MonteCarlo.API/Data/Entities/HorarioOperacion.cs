namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Franjas de atencion por dia de semana (HU-CFG-005).
/// </summary>
public class HorarioOperacion
{
    public int IdHorarioOperacion { get; set; }

    /// <summary>0 = Domingo ... 6 = Sabado.</summary>
    public byte DiaSemana { get; set; }
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }
    public bool Activo { get; set; } = true;
}
