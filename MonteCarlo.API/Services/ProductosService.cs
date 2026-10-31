using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class ProductosService(
    IProductosRepository productosRepository,
    ICategoriasProductoRepository categoriasRepository,
    IHistorialCatalogoRepository historialRepository) : IProductosService
{
    private const string NombreDuplicado = "Ya existe un producto con ese nombre.";

    public async Task<Result<List<ProductoResponse>>> ObtenerTodosAsync(string? busqueda, int? idCategoria, bool? disponible)
    {
        var productos = await productosRepository.ObtenerTodosAsync(busqueda, idCategoria, disponible);
        var conVentas = await productosRepository.ObtenerIdsConVentasAsync();
        return Result<List<ProductoResponse>>.Ok(
            productos.Select(p => Mapear(p, eliminable: !conVentas.Contains(p.IdProducto))).ToList(), "Productos obtenidos exitosamente.");
    }

    public async Task<Result<ProductoResponse>> ObtenerPorIdAsync(int idProducto)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(idProducto);
        return producto is null
            ? Result<ProductoResponse>.NotFound("El producto no existe.")
            : Result<ProductoResponse>.Ok(Mapear(producto, eliminable: !await productosRepository.TieneVentasAsync(idProducto)), "Producto obtenido exitosamente.");
    }

    public async Task<Result<ProductoResponse>> CrearAsync(GuardarProductoRequest request, int idUsuario)
    {
        var validacion = await ValidarAsync(request, existente: null);
        if (validacion.Error is not null)
            return validacion.Error;

        var producto = new Producto
        {
            Nombre = validacion.Nombre,
            Precio = validacion.Precio,
            Categoria = validacion.Categoria,
            Disponible = true
        };

        productosRepository.Agregar(producto);
        historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Creado));

        if (!await GuardarAsync())
            return Result<ProductoResponse>.Conflict(NombreDuplicado);

        return new Result<ProductoResponse>(true, $"El producto «{producto.Nombre}» fue registrado y está disponible.", Mapear(producto), 201);
    }

    public async Task<Result<ProductoResponse>> ActualizarAsync(int idProducto, GuardarProductoRequest request, int idUsuario)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(idProducto);
        if (producto is null)
            return Result<ProductoResponse>.NotFound("El producto no existe.");

        var validacion = await ValidarAsync(request, producto);
        if (validacion.Error is not null)
            return validacion.Error;

        var cambios = 0;

        if (producto.Nombre != validacion.Nombre)
        {
            historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Editado,
                HistorialCatalogoCampos.Nombre, producto.Nombre, validacion.Nombre));
            producto.Nombre = validacion.Nombre;
            cambios++;
        }

        if (producto.Precio != validacion.Precio)
        {
            historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Editado,
                HistorialCatalogoCampos.Precio, FormatearPrecio(producto.Precio), FormatearPrecio(validacion.Precio)));
            producto.Precio = validacion.Precio;
            cambios++;
        }

        if (producto.IdCategoria != validacion.Categoria.IdCategoria)
        {
            historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Editado,
                HistorialCatalogoCampos.Categoria, producto.Categoria.Nombre, validacion.Categoria.Nombre));
            producto.Categoria = validacion.Categoria;
            cambios++;
        }

        if (cambios == 0)
            return Result<ProductoResponse>.Ok(Mapear(producto), "No hay cambios que guardar.");

        if (!await GuardarAsync())
            return Result<ProductoResponse>.Conflict(NombreDuplicado);

        return Result<ProductoResponse>.Ok(Mapear(producto), "El producto fue actualizado.");
    }

    public async Task<Result<ProductoResponse>> DeshabilitarAsync(int idProducto, int idUsuario)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(idProducto);
        if (producto is null)
            return Result<ProductoResponse>.NotFound("El producto no existe.");

        if (!producto.Disponible)
            return Result<ProductoResponse>.Conflict("El producto ya está deshabilitado.");

        producto.Disponible = false;
        historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Deshabilitado));
        await productosRepository.GuardarCambiosAsync();

        return Result<ProductoResponse>.Ok(Mapear(producto),
            $"El producto «{producto.Nombre}» fue deshabilitado. El historial de ventas se conserva.");
    }

    public async Task<Result<ProductoResponse>> EliminarAsync(int idProducto, int idUsuario)
    {
        var producto = await productosRepository.ObtenerParaEliminarAsync(idProducto);
        if (producto is null)
            return Result<ProductoResponse>.NotFound("El producto no existe.");

        const string conVentas = "tiene ventas registradas y no se puede eliminar. Puedes deshabilitarlo.";
        if (await productosRepository.TieneVentasAsync(idProducto))
            return Result<ProductoResponse>.Conflict($"El producto «{producto.Nombre}» {conVentas}");

        var respuesta = Mapear(producto, eliminable: true);

        // El historial queda con el nombre guardado; la FK pasa a NULL al borrar (ON DELETE SET NULL).
        foreach (var entrada in producto.Historial)
            entrada.NombreElemento = producto.Nombre;

        historialRepository.Agregar(new HistorialCatalogo
        {
            TipoElemento = HistorialCatalogoTipos.Producto,
            NombreElemento = producto.Nombre,
            IdUsuario = idUsuario,
            Fecha = DateTime.Now,
            Accion = HistorialCatalogoAcciones.Eliminado,
            Motivo = $"Categoría «{producto.Categoria.Nombre}»"
        });

        productosRepository.Eliminar(producto);

        try
        {
            await productosRepository.GuardarCambiosAsync();
        }
        catch (DbUpdateException)
        {
            // Una venta lo uso entre la comprobacion y el borrado: la FK de DetalleVenta lo impide.
            return Result<ProductoResponse>.Conflict($"El producto «{producto.Nombre}» {conVentas}");
        }

        return Result<ProductoResponse>.Ok(respuesta, $"El producto «{respuesta.Nombre}» fue eliminado.");
    }

    public async Task<Result<ProductoResponse>> HabilitarAsync(int idProducto, bool habilitarCategoria, int idUsuario)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(idProducto);
        if (producto is null)
            return Result<ProductoResponse>.NotFound("El producto no existe.");

        if (producto.Disponible)
            return Result<ProductoResponse>.Conflict("El producto ya está disponible.");

        var categoria = producto.Categoria;
        var habilitaCategoria = false;

        if (!categoria.Activa)
        {
            if (!habilitarCategoria)
            {
                return new Result<ProductoResponse>(false,
                    $"La categoría «{categoria.Nombre}» está deshabilitada. Para habilitar el producto hay que habilitar también la categoría.",
                    default, 409, new { idCategoria = categoria.IdCategoria, categoria = categoria.Nombre });
            }

            categoria.Activa = true;
            habilitaCategoria = true;
            historialRepository.Agregar(HistorialCatalogoFactory.DeCategoria(categoria, idUsuario, HistorialCatalogoAcciones.Habilitado,
                motivo: $"Por habilitar el producto «{producto.Nombre}»"));
        }

        producto.Disponible = true;
        historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Habilitado,
            motivo: habilitaCategoria ? $"Su categoría «{categoria.Nombre}» estaba deshabilitada y se habilitó" : null));
        await productosRepository.GuardarCambiosAsync();

        return Result<ProductoResponse>.Ok(Mapear(producto), habilitaCategoria
            ? $"El producto «{producto.Nombre}» y la categoría «{categoria.Nombre}» fueron habilitados."
            : $"El producto «{producto.Nombre}» está disponible nuevamente.");
    }

    // ------------------------------------------------------------
    // Soporte
    // ------------------------------------------------------------

    private sealed record Validado(Result<ProductoResponse>? Error, string Nombre, decimal Precio, CategoriaProducto Categoria);

    private async Task<Validado> ValidarAsync(GuardarProductoRequest request, Producto? existente)
    {
        Validado Falla(Result<ProductoResponse> error) => new(error, "", 0, null!);

        var nombre = request.Nombre?.Trim() ?? "";
        if (nombre.Length == 0)
            return Falla(Result<ProductoResponse>.BadRequest("El nombre es obligatorio."));

        if (nombre.Length > 100)
            return Falla(Result<ProductoResponse>.BadRequest("El nombre no puede superar los 100 caracteres."));

        if (request.Precio is null)
            return Falla(Result<ProductoResponse>.BadRequest("El precio es obligatorio."));

        var precio = request.Precio.Value;
        if (precio < 0 || precio > 99999999.99m)
            return Falla(Result<ProductoResponse>.BadRequest("El precio debe estar entre 0 y 99 999 999,99 colones."));

        if (decimal.Round(precio, 2) != precio)
            return Falla(Result<ProductoResponse>.BadRequest("El precio admite como máximo 2 decimales."));

        if (request.IdCategoria is null)
            return Falla(Result<ProductoResponse>.BadRequest("La categoría es obligatoria."));

        var categoria = await categoriasRepository.ObtenerPorIdAsync(request.IdCategoria.Value);
        if (categoria is null)
            return Falla(Result<ProductoResponse>.BadRequest("La categoría seleccionada no existe."));

        if (await productosRepository.ExisteNombreAsync(nombre, existente?.IdProducto))
            return Falla(Result<ProductoResponse>.Conflict(NombreDuplicado));

        // Una categoria deshabilitada no admite productos nuevos ni movimientos hacia ella.
        // Si el producto ya estaba en ella y no cambia de categoria, se permite editarlo.
        if (!categoria.Activa && existente?.IdCategoria != categoria.IdCategoria)
            return Falla(Result<ProductoResponse>.BadRequest($"La categoría «{categoria.Nombre}» está deshabilitada. Elige una categoría activa."));

        return new Validado(null, nombre, precio, categoria);
    }

    /// Guarda y devuelve false si otro administrador registro el mismo nombre un instante antes (indice unico).
    private async Task<bool> GuardarAsync()
    {
        try
        {
            await productosRepository.GuardarCambiosAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_Producto_Nombre") == true)
        {
            return false;
        }
    }

    private static string FormatearPrecio(decimal precio) => precio.ToString("0.##", CultureInfo.InvariantCulture);

    private static ProductoResponse Mapear(Producto p, bool eliminable = false) => new()
    {
        IdProducto = p.IdProducto,
        Nombre = p.Nombre,
        Precio = p.Precio,
        Disponible = p.Disponible,
        IdCategoria = p.Categoria.IdCategoria,
        Categoria = p.Categoria.Nombre,
        CategoriaActiva = p.Categoria.Activa,
        Eliminable = eliminable
    };
}
