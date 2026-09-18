namespace MonteCarlo.WEB.Models;

/// <summary>
/// Claves de TempData que reconoce el parcial _Alerts.
/// Se usan constantes en lugar de cadenas sueltas para que un
/// error de escritura sea un error de compilacion y no un mensaje
/// que desaparece en silencio.
/// </summary>
public static class Alerta
{
    /// <summary>Operacion completada. Se descarta sola.</summary>
    public const string Exito = "Alerta.Exito";

    /// <summary>Operacion rechazada o fallida. Permanece visible.</summary>
    public const string Error = "Alerta.Error";

    /// <summary>Advertencia que no impide continuar. Permanece visible.</summary>
    public const string Aviso = "Alerta.Aviso";

    /// <summary>Informacion neutra. Se descarta sola.</summary>
    public const string Info = "Alerta.Info";
}
