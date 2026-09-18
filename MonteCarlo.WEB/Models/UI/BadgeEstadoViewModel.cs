namespace MonteCarlo.WEB.Models.UI;

/// <summary>Tono visual de un badge de estado.</summary>
public enum TonoBadge
{
    Neutral,
    Exito,
    Advertencia,
    Peligro,
    Informativo
}

/// <summary>
/// Etiqueta de estado. El texto siempre acompana al color: el
/// color por si solo no comunica el estado a quien no lo
/// distingue (WCAG 1.4.1).
///
/// Los metodos de fabrica concentran en un solo lugar la relacion
/// estado -> color, para que la misma "Activa" no aparezca verde
/// en una pantalla y gris en otra.
/// </summary>
public class BadgeEstadoViewModel
{
    public required string Texto { get; init; }

    public TonoBadge Tono { get; init; } = TonoBadge.Neutral;

    public string ClaseCss => Tono switch
    {
        TonoBadge.Exito => "mc-badge mc-badge--success",
        TonoBadge.Advertencia => "mc-badge mc-badge--warning",
        TonoBadge.Peligro => "mc-badge mc-badge--danger",
        TonoBadge.Informativo => "mc-badge mc-badge--info",
        _ => "mc-badge mc-badge--neutral"
    };

    /// <summary>
    /// Estado de una cuenta de administrador (HU-AUT-006).
    /// </summary>
    public static BadgeEstadoViewModel Cuenta(bool activa) => activa
        ? new BadgeEstadoViewModel { Texto = "Activa", Tono = TonoBadge.Exito }
        : new BadgeEstadoViewModel { Texto = "Inactiva", Tono = TonoBadge.Neutral };

    /// <summary>
    /// Estado de una reserva. Aun sin pantallas que lo usen; queda
    /// definido aqui para que los modulos RES y OPE no vuelvan a
    /// elegir colores por su cuenta.
    /// </summary>
    public static BadgeEstadoViewModel Reserva(string estado) => new()
    {
        Texto = estado,
        Tono = estado switch
        {
            "Confirmada" => TonoBadge.Exito,
            "Pendiente" => TonoBadge.Advertencia,
            "Cancelada" => TonoBadge.Peligro,
            "No Show" => TonoBadge.Peligro,
            "Finalizada" => TonoBadge.Informativo,
            _ => TonoBadge.Neutral
        }
    };
}
