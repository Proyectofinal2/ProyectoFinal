namespace MonteCarlo.API.DTOs;

/// <summary>Una fila del historial de cambios de los horarios de operacion.</summary>
public class HistorialHorarioOperacionResponse
{
    public int IdHistorialHorarioOperacion { get; set; }
    public Guid IdOperacion { get; set; }
    public int IdHorarioOperacion { get; set; }
    public int DiaSemana { get; set; }
    public string NombreDia { get; set; } = null!;
    public string Accion { get; set; } = null!;
    public TimeOnly? HoraAperturaAnterior { get; set; }
    public TimeOnly HoraAperturaNueva { get; set; }
    public TimeOnly? HoraCierreAnterior { get; set; }
    public TimeOnly HoraCierreNuevo { get; set; }
    public bool? ActivoAnterior { get; set; }
    public bool ActivoNuevo { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = null!;
    public string RolUsuario { get; set; } = null!;
    public DateTime Fecha { get; set; }
}
