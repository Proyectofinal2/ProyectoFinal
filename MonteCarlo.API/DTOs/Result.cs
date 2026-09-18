namespace MonteCarlo.API.DTOs;

/// <summary>
/// Resultado genérico de una operación en el servicio.
/// Reemplaza los tuples complejos con un tipo expresivo y fácil de leer.
/// Se usa en la capa de servicios, no se expone al cliente.
/// </summary>
/// <typeparam name="T">Tipo de dato que retorna la operación si es exitosa.</typeparam>
public class Result<T>
{
    /// <summary>true si la operación fue exitosa, false si falló.</summary>
    public bool Success { get; set; }

    /// <summary>Mensaje descriptivo del resultado o error.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Datos de la respuesta (puede ser null si falla la operación).</summary>
    public T? Data { get; set; }

    /// <summary>Código HTTP de la respuesta (200, 400, 401, 500, etc).</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Datos adicionales sobre el error, cuando el mensaje por sí solo no alcanza
    /// (ej. segundos restantes de un bloqueo temporal). Null en el resto de los casos.
    /// </summary>
    public object? ErrorData { get; set; }

    // ========== Constructores ==========

    public Result() { }

    public Result(bool success, string message, T? data = default, int statusCode = 200, object? errorData = null)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
        ErrorData = errorData;
    }

    // ========== Factory Methods ==========

    /// <summary>Crea un resultado exitoso con datos (200 OK).</summary>
    public static Result<T> Ok(T data, string message = "Operación exitosa")
        => new(true, message, data, 200);

    /// <summary>Crea un resultado de solicitud inválida (400 Bad Request).</summary>
    public static Result<T> BadRequest(string message)
        => new(false, message, default, 400);

    /// <summary>Crea un resultado de no autenticado (401 Unauthorized).</summary>
    public static Result<T> Unauthorized(string message = "No autorizado", object? errorData = null)
        => new(false, message, default, 401, errorData);

    /// <summary>Crea un resultado de recurso no encontrado (404 Not Found).</summary>
    public static Result<T> NotFound(string message = "Recurso no encontrado")
        => new(false, message, default, 404);

    /// <summary>Crea un resultado de conflicto/duplicado (409 Conflict).</summary>
    public static Result<T> Conflict(string message)
        => new(false, message, default, 409);

    /// <summary>Crea un resultado de error interno del servidor (500 Internal Server Error).</summary>
    public static Result<T> InternalError(string message = "Error interno del servidor")
        => new(false, message, default, 500);
}
