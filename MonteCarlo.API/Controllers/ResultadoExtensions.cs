using Microsoft.AspNetCore.Mvc;
using MonteCarlo.API.DTOs;

namespace MonteCarlo.API.Controllers;

/// <summary>Convierte el <see cref="Result{T}"/> de un servicio en la respuesta estandar <see cref="ApiResponse"/>.</summary>
internal static class ResultadoExtensions
{
    public static IActionResult Responder<T>(this ControllerBase controller, Result<T> result) =>
        controller.StatusCode(result.StatusCode, result.Success
            ? ApiResponse.SuccessResponse(result.Message, result.Data)
            : ApiResponse.ErrorResponse(result.Message, result.ErrorData));
}
