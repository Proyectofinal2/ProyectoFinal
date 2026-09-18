namespace MonteCarlo.API.DTOs;

/// <summary>
/// Respuesta estándar de la API para cualquier endpoint.
/// Permite comunicar errores y datos de forma consistente.
/// </summary>
public class ApiResponse
{
    /// <summary>true si la operación fue exitosa, false si falló.</summary>
    public bool Success { get; set; } = true;

    /// <summary>Mensaje descriptivo del resultado o error.</summary>
    public string Message { get; set; } = "Operación exitosa";

    /// <summary>Datos de la respuesta (puede ser null).</summary>
    public object? Data { get; set; }

    /// <summary>Lista de errores de validación, si las hay.</summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>Inicializa una nueva instancia de <see cref="ApiResponse"/>.</summary>
    public ApiResponse()
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ApiResponse"/> con los valores indicados.
    /// </summary>
    /// <param name="success">true si la operación fue exitosa, false si falló.</param>
    /// <param name="message">Mensaje descriptivo del resultado o error.</param>
    /// <param name="data">Datos de la respuesta (puede ser null).</param>
    public ApiResponse(bool success, string message, object? data = null)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    /// <summary>
    /// Crea una respuesta de éxito con mensaje opcional y datos.
    /// </summary>
    /// <param name="message">Mensaje descriptivo (por defecto "Operación exitosa").</param>
    /// <param name="data">Datos de la respuesta (opcional).</param>
    /// <returns>Una instancia de <see cref="ApiResponse"/> con Success = true.</returns>
    public static ApiResponse SuccessResponse(string message = "Operación exitosa", object? data = null)
        => new(true, message, data);

    /// <summary>
    /// Crea una respuesta de error con el mensaje especificado.
    /// </summary>
    /// <param name="message">Mensaje de error a devolver.</param>
    /// <param name="data">Datos adicionales sobre el error, si los hay (opcional).</param>
    /// <returns>Una instancia de <see cref="ApiResponse"/> con Success = false.</returns>
    public static ApiResponse ErrorResponse(string message, object? data = null)
        => new(false, message, data);

    /// <summary>
    /// Crea una respuesta que representa errores de validación.
    /// </summary>
    /// <param name="errors">Diccionario con los errores de validación.</param>
    /// <returns>Una instancia de <see cref="ApiResponse"/> con Success = false y la colección de errores.</returns>
    public static ApiResponse ValidationErrorResponse(Dictionary<string, string[]> errors)
        => new(false, "La solicitud contiene errores de validación")
        {
            Errors = errors
        };
}
