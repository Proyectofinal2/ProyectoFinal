using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MonteCarlo.WEB.Services;

/// <summary>
/// Handler que automáticamente agrega el Bearer token JWT a todas las solicitudes HTTP.
/// Se ejecuta antes de enviar cada request al API.
/// </summary>
public class AuthTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Extraer el token JWT del contexto HTTP (guardado como claim en la cookie)
        var token = httpContextAccessor.HttpContext?.User
            .FindFirst("access_token")?.Value;

        if (!string.IsNullOrWhiteSpace(token))
        {
            // Agregar el token como Bearer en el header Authorization
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
