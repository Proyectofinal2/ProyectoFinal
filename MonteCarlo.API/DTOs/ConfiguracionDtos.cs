using System.ComponentModel.DataAnnotations;

namespace MonteCarlo.API.DTOs;

/// <summary>Un parámetro de ConfiguracionSistema tal como existe en la base de datos.
/// La pantalla dinámica de Parametros no conoce un set fijo de claves: pinta lo que
/// esta respuesta traiga.</summary>
public class ParametroConfiguracionResponse
{
    public string Clave { get; set; } = string.Empty;

    /// <summary>Nombre amigable (columna Etiqueta). Si viene null, es la clave misma.</summary>
    public string Etiqueta { get; set; } = string.Empty;

    /// <summary>Texto de ayuda bajo el input (columna Descripcion). Null si no se definió.</summary>
    public string? Descripcion { get; set; }

    public string Valor { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}

/// <summary>Todos los parametros existentes en ConfiguracionSistema, para el GET/PUT
/// unico de la pantalla de Parametros.</summary>
public class ConfiguracionGeneralResponse
{
    public List<ParametroConfiguracionResponse> Parametros { get; set; } = [];
}

/// <summary>Un parámetro a actualizar, por su clave de ConfiguracionSistema. El rango
/// válido según la clave se valida en el servicio, no aquí.</summary>
public class ActualizarParametroItem
{
    [Required(ErrorMessage = "La clave del parámetro es obligatoria.")]
    public string Clave { get; set; } = string.Empty;

    [Required(ErrorMessage = "El valor del parámetro es obligatorio.")]
    public string Valor { get; set; } = string.Empty;
}

/// <summary>PATCH simplificado: solo los parámetros que realmente cambiaron, para no
/// pisar un parámetro que otro admin pudo haber modificado mientras tanto.</summary>
public class ActualizarConfiguracionGeneralRequest
{
    public List<ActualizarParametroItem> Parametros { get; set; } = [];
}
