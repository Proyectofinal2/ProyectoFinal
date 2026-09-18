using System.Text.Json;

namespace MonteCarlo.WEB.Services;

/// <summary>
/// Parsea el envelope de respuesta genérica de la API ({ success, message, data, errors }).
/// No tiene relación con <see cref="Alerta"/> (claves de TempData) ni con
/// _Alerts.cshtml (el parcial que las renderiza) — aquí solo se traduce
/// el HTTP response al tuple que consumen los servicios cliente.
/// </summary>
public static class ApiResponseHelper
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<(bool Success, string Message, T? Data, JsonElement? ErrorData)> ParseAsync<T>(
        HttpResponseMessage response,
        string successMessage,
        string errorMessage)
    {
        var content = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponseWrapper>(content, Options);

        if (response.IsSuccessStatusCode && envelope?.Success == true)
        {
            var data = envelope.Data != null
                ? JsonSerializer.Deserialize<T>(envelope.Data.ToString() ?? "{}", Options)
                : default;
            return (true, envelope.Message ?? successMessage, data, null);
        }

        var errorData = envelope?.Data is JsonElement { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } je
            ? je
            : (JsonElement?)null;

        return (false, envelope?.Message ?? errorMessage, default, errorData);
    }

    private class ApiResponseWrapper
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public object? Data { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
