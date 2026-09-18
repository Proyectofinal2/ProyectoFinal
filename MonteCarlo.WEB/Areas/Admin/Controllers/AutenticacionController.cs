using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-AUT-001 (login), HU-AUT-002 (logout) y HU-AUT-003
/// (recuperacion de contrasena).
///
/// ALCANCE ACTUAL: login y logout implementados con conexión a la API.
/// Recuperación de contraseña aún es TODO.
/// </summary>
[Area("Admin")]
[Route("admin")]
public class AutenticacionController(IAuthApiService authApiService, ILogger<AutenticacionController> logger)
    : Controller
{
    // ------------------------------------------------------------
    // HU-AUT-001 — Inicio de sesion
    // ------------------------------------------------------------

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Login));

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // Autenticar contra la API
        var (success, message, loginResponse, segundosBloqueoRestantes) = await authApiService.LoginAsync(
            modelo.Identificador,
            modelo.Contrasena
        );

        if (!success)
        {
            // HU-AUT-001, escenario 3: bloqueo temporal, con los segundos
            // reales que devuelve la API
            if (segundosBloqueoRestantes is > 0)
            {
                modelo.SegundosBloqueoRestantes = segundosBloqueoRestantes;
                return View(modelo);
            }

            // Escenario 2: credenciales inválidas (error genérico a nivel de formulario)
            ModelState.AddModelError(string.Empty, message);
            return View(modelo);
        }

        // HU-AUT-001, escenario 1: login exitoso
        // Crear los claims para la cookie de autenticación
        var claims = new List<Claim>
        {
            new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, loginResponse!.IdUsuario.ToString()),
            new Claim(System.Security.Claims.ClaimTypes.Name, loginResponse.NombreCompleto),
            new Claim(System.Security.Claims.ClaimTypes.Email, loginResponse.CorreoElectronico),
            new Claim(System.Security.Claims.ClaimTypes.Role, loginResponse.Rol),
            new Claim("access_token", loginResponse.Token)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        logger.LogInformation($"Usuario {loginResponse.NombreCompleto} ({loginResponse.CorreoElectronico}) inició sesión.");

        // Redirigir a ReturnUrl si es local, o al panel
        if (Url.IsLocalUrl(modelo.ReturnUrl))
        {
            return Redirect(modelo.ReturnUrl);
        }

        return RedirectToAction(nameof(PanelController.Index), "Panel");
    }

    // ------------------------------------------------------------
    // HU-AUT-002 — Cierre de sesion
    // ------------------------------------------------------------

    /// <summary>
    /// Solo por POST y con token antiforgery. Si fuese un GET,
    /// bastaria con que alguien incrustara una imagen apuntando a
    /// esta ruta para cerrarle la sesion al administrador.
    /// </summary>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // HU-AUT-002: cerrar sesión invalidando la cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        logger.LogInformation($"Usuario {User.FindFirst(ClaimTypes.Name)?.Value} cerró sesión.");

        // Impide que el botón Atrás del navegador muestre una página
        // del panel desde la cache después de cerrar sesión.
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";

        TempData[Alerta.Exito] = "Cerraste sesión correctamente.";
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------------
    // HU-AUT-003 — Recuperacion de contrasena
    // ------------------------------------------------------------

    [HttpGet("recuperar")]
    public IActionResult Recuperar() => View(new RecuperarContrasenaViewModel());

    [HttpPost("recuperar")]
    [ValidateAntiForgeryToken]
    public IActionResult Recuperar(RecuperarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // TODO conectar: solicitar envio del enlace a la API. No confirmar ni negar
        // si el correo existe: siempre terminar aqui, con tiempo de respuesta similar.

        return RedirectToAction(nameof(RecuperarEnviado));
    }

    /// <summary>
    /// Pantalla de confirmacion. Es una accion aparte y no una
    /// alerta sobre el mismo formulario para que recargar la
    /// pagina no reenvie la solicitud (patron POST-Redirect-GET).
    /// </summary>
    [HttpGet("recuperar/enviado")]
    public IActionResult RecuperarEnviado() => View();


    /// <param name="token">Llega en el enlace enviado por correo.</param>
    [HttpGet("restablecer")]
    public IActionResult Restablecer(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(EnlaceInvalido));
        }

        // TODO conectar: validar el token antes de mostrar el formulario;
        // si expiro o ya se uso, redirigir a EnlaceInvalido.

        return View(new RestablecerContrasenaViewModel { Token = token });
    }

    [HttpPost("restablecer")]
    [ValidateAntiForgeryToken]
    public IActionResult Restablecer(RestablecerContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // TODO conectar: aplicar contrasena nueva e invalidar token.
        // Si la API rechaza el token, redirigir a EnlaceInvalido.

        TempData[Alerta.Exito] =
            "Tu contrasena fue actualizada. Ya puedes iniciar sesion con ella.";

        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Escenario 3: enlace expirado o ya utilizado. Pantalla propia
    /// en lugar de una alerta, porque el usuario necesita una salida
    /// clara (solicitar otro enlace), no solo enterarse del fallo.
    /// </summary>
    [HttpGet("restablecer/invalido")]
    public IActionResult EnlaceInvalido() => View();
}
