using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonteCarlo.WEB.Models;
using MonteCarlo.WEB.Models.Autenticacion;
using MonteCarlo.WEB.Models.Catalogo;
using MonteCarlo.WEB.Services.Interfaces;

namespace MonteCarlo.WEB.Areas.Admin.Controllers;

/// <summary>
/// HU-CON-001: catalogo de productos. Lo gestiona cualquier administrador.
/// El panel lateral (alta y edicion) se pide por fetch a Nuevo / Editar, que devuelven solo el formulario;
/// si el POST falla por validacion se vuelve a pintar la pagina con el panel abierto y los errores.
/// </summary>
[Area("Admin")]
[Route("admin/productos")]
[Authorize(Roles = "General,Regular")]
public class ProductosController(ICatalogoApiService catalogoApiService) : Controller
{
    private bool EsAdministradorGeneral => User.IsInRole(Roles.General);

    // ------------------------------------------------------------
    // Listado
    // ------------------------------------------------------------

    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, int? idCategoria, string? estado) =>
        View(await ConstruirIndexAsync(busqueda, idCategoria, estado));

    // ------------------------------------------------------------
    // Panel lateral (devuelven solo el formulario)
    // ------------------------------------------------------------

    [HttpGet("nuevo")]
    public async Task<IActionResult> Nuevo(int? idCategoria)
    {
        var formulario = new ProductoFormViewModel { IdCategoria = idCategoria };
        return await CargarCategoriasAsync(formulario) ? PartialView("_ProductoFormulario", formulario) : StatusCode(503);
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id)
    {
        var (success, _, producto) = await catalogoApiService.ObtenerProductoAsync(id);
        if (!success || producto is null)
            return NotFound();

        var formulario = new ProductoFormViewModel
        {
            IdProducto = producto.IdProducto,
            Nombre = producto.Nombre,
            Precio = Colones.ParaCampo(producto.Precio),
            IdCategoria = producto.IdCategoria
        };

        if (!await CargarCategoriasAsync(formulario))
            return StatusCode(503);

        await CargarHistorialAsync(formulario);
        return PartialView("_ProductoFormulario", formulario);
    }

    // ------------------------------------------------------------
    // HU-CON-001, escenarios 1 y 2 — Alta y edicion
    // ------------------------------------------------------------

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProductoFormViewModel modelo)
    {
        if (ModelState.IsValid)
        {
            Colones.TryParse(modelo.Precio, out var precio);
            var (success, message, _) = await catalogoApiService.CrearProductoAsync(modelo.Nombre.Trim(), precio, modelo.IdCategoria!.Value);

            if (success)
            {
                TempData[Alerta.Exito] = message;
                return RedirectToAction(nameof(Index));
            }

            AsignarError(message);
        }

        return await IndexConFormularioAsync(modelo);
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int id, ProductoFormViewModel modelo)
    {
        modelo.IdProducto = id;

        if (ModelState.IsValid)
        {
            Colones.TryParse(modelo.Precio, out var precio);
            var (success, message, _) = await catalogoApiService.ActualizarProductoAsync(id, modelo.Nombre.Trim(), precio, modelo.IdCategoria!.Value);

            if (success)
            {
                TempData[Alerta.Exito] = message;
                return VolverAlListado();
            }

            AsignarError(message);
        }

        return await IndexConFormularioAsync(modelo);
    }

    // ------------------------------------------------------------
    // HU-CON-001, escenario 3 — Disponibilidad
    // ------------------------------------------------------------

    [HttpPost("{id:int}/deshabilitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deshabilitar(int id)
    {
        var (success, message, _) = await catalogoApiService.DeshabilitarProductoAsync(id);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return VolverAlListado();
    }

    /// <summary>Elimina el producto solo si no tiene ventas (la API lo valida); su historial se conserva.</summary>
    [HttpPost("{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var (success, message) = await catalogoApiService.EliminarProductoAsync(id);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return VolverAlListado();
    }

    /// <param name="habilitarCategoria">true cuando el administrador confirmo, en el dialogo, habilitar tambien la categoria.</param>
    [HttpPost("{id:int}/habilitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Habilitar(int id, bool habilitarCategoria = false)
    {
        var (success, message, _) = await catalogoApiService.HabilitarProductoAsync(id, habilitarCategoria);
        TempData[success ? Alerta.Exito : Alerta.Error] = message;
        return VolverAlListado();
    }

    // ------------------------------------------------------------
    // Soporte
    // ------------------------------------------------------------

    private async Task<ProductosIndexViewModel> ConstruirIndexAsync(string? busqueda, int? idCategoria, string? estado)
    {
        var modelo = new ProductosIndexViewModel { Busqueda = busqueda, IdCategoria = idCategoria, Estado = estado };

        var (okCategorias, mensajeCategorias, categorias) = await catalogoApiService.ObtenerCategoriasAsync();
        var (okProductos, mensajeProductos, productos) = await catalogoApiService.ObtenerProductosAsync(
            busqueda, idCategoria, estado switch { "disponible" => true, "deshabilitado" => false, _ => null });

        if (okCategorias && okProductos)
        {
            modelo.Categorias = categorias ?? [];
            modelo.Productos = productos ?? [];
        }
        else
        {
            TempData[Alerta.Error] = okCategorias ? mensajeProductos : mensajeCategorias;
        }

        return modelo;
    }

    private async Task<IActionResult> IndexConFormularioAsync(ProductoFormViewModel formulario)
    {
        var modelo = await ConstruirIndexAsync(null, null, null);

        await CargarCategoriasAsync(formulario);
        if (formulario.EsEdicion)
            await CargarHistorialAsync(formulario);

        modelo.Formulario = formulario;
        return View("Index", modelo);
    }

    /// <summary>Categorias elegibles: las activas y, al editar, la actual aunque este deshabilitada.</summary>
    private async Task<bool> CargarCategoriasAsync(ProductoFormViewModel formulario)
    {
        var (success, _, categorias) = await catalogoApiService.ObtenerCategoriasAsync();
        if (!success)
            return false;

        formulario.Categorias = (categorias ?? [])
            .Where(c => c.Activa || c.IdCategoria == formulario.IdCategoria)
            .ToList();
        return true;
    }

    /// <summary>El historial del producto solo lo ve el Administrador General; la API lo exige tambien.</summary>
    private async Task CargarHistorialAsync(ProductoFormViewModel formulario)
    {
        if (!EsAdministradorGeneral)
            return;

        var (success, _, historial) = await catalogoApiService.ObtenerHistorialProductoAsync(formulario.IdProducto);
        formulario.Historial = success ? historial ?? [] : [];
    }

    /// <summary>Lleva el mensaje de la API al campo que lo origino; si no se reconoce, queda como error general.</summary>
    private void AsignarError(string mensaje)
    {
        var texto = mensaje.ToLowerInvariant();
        var campo = texto.Contains("nombre") ? nameof(ProductoFormViewModel.Nombre)
            : texto.Contains("precio") ? nameof(ProductoFormViewModel.Precio)
            : texto.Contains("categoría") ? nameof(ProductoFormViewModel.IdCategoria)
            : "";

        ModelState.AddModelError(campo, mensaje);
    }

    /// <summary>Regresa al listado con los mismos filtros (desde el Referer) o al listado limpio.</summary>
    private IActionResult VolverAlListado()
    {
        if (Uri.TryCreate(Request.Headers.Referer.ToString(), UriKind.Absolute, out var referer)
            && string.Equals(referer.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase)
            && referer.AbsolutePath.StartsWith("/admin/productos", StringComparison.OrdinalIgnoreCase)
            && Url.IsLocalUrl(referer.PathAndQuery))
        {
            return LocalRedirect(referer.PathAndQuery);
        }

        return RedirectToAction(nameof(Index));
    }
}
