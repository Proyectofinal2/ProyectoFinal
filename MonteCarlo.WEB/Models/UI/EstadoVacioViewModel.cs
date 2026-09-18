namespace MonteCarlo.WEB.Models.UI;

/// <summary>
/// Mensaje que ocupa el lugar de una lista sin resultados.
///
/// Varias historias lo exigen de forma explicita: HU-AUT-006,
/// HU-MES-002, HU-REP-001/002, HU-CFG-004, HU-OPE-001 y
/// HU-CON-004. Un unico parcial evita que cada modulo invente su
/// propia version.
///
/// Distingue dos situaciones que no deben verse igual:
/// "aun no hay nada" (invita a crear) y "tu busqueda no arrojo
/// resultados" (invita a cambiar los filtros).
/// </summary>
public class EstadoVacioViewModel
{
    /// <summary>Icono de Bootstrap Icons, sin el prefijo "bi ".</summary>
    public string Icono { get; init; } = "bi-inbox";

    public string Titulo { get; init; } = "No hay informacion disponible";

    /// <summary>Explica el motivo y, si aplica, que hacer al respecto.</summary>
    public string? Descripcion { get; init; }

    /// <summary>Texto del boton de accion. Si es nulo no se muestra boton.</summary>
    public string? TextoAccion { get; init; }

    /// <summary>Destino del boton de accion.</summary>
    public string? UrlAccion { get; init; }

    /// <summary>
    /// Estado vacio por busqueda sin coincidencias: ofrece limpiar
    /// los filtros en lugar de crear un registro nuevo.
    /// </summary>
    public static EstadoVacioViewModel SinResultados(string? urlLimpiar = null) => new()
    {
        Icono = "bi-search",
        Titulo = "Sin coincidencias",
        Descripcion = "No encontramos registros que cumplan los criterios seleccionados. Prueba a ampliar o quitar algun filtro.",
        TextoAccion = urlLimpiar is null ? null : "Limpiar filtros",
        UrlAccion = urlLimpiar
    };
}
