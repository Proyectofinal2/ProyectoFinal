namespace MonteCarlo.API.Data.Entities;

/// <summary>
/// Auditoria de cada cambio efectivo realizado sobre un horario de operacion.
/// Las filas de un mismo guardado por lote comparten <see cref="IdOperacion"/>.
/// </summary>
public class HistorialHorarioOperacion
{
    public int IdHistorialHorarioOperacion { get; set; }
    public Guid IdOperacion { get; set; }
    public int IdHorarioOperacion { get; set; }
    public byte DiaSemana { get; set; }
    public string Accion { get; set; } = null!;
    public TimeOnly? HoraAperturaAnterior { get; set; }
    public TimeOnly HoraAperturaNueva { get; set; }
    public TimeOnly? HoraCierreAnterior { get; set; }
    public TimeOnly HoraCierreNuevo { get; set; }
    public bool? ActivoAnterior { get; set; }
    public bool ActivoNuevo { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; }

    public HorarioOperacion HorarioOperacion { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public static class HistorialHorarioOperacionAcciones
{
    public const string Creado = "Creado";
    public const string Modificado = "Modificado";

    public static readonly string[] Todas = [Creado, Modificado];
}
