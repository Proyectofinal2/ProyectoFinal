using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class CategoriasProductoService(
    ICategoriasProductoRepository categoriasRepository,
    IHistorialCatalogoRepository historialRepository) : ICategoriasProductoService
{
    private const string NombreDuplicado = "Ya existe una categoría con ese nombre.";

    public async Task<Result<List<CategoriaProductoResponse>>> ObtenerTodasAsync()
    {
        var categorias = await categoriasRepository.ObtenerTodasAsync();
        return Result<List<CategoriaProductoResponse>>.Ok(categorias.Select(Mapear).ToList(), "Categorías obtenidas exitosamente.");
    }

    public async Task<Result<CategoriaProductoResponse>> CrearAsync(GuardarCategoriaProductoRequest request, int idUsuario)
    {
        var (nombre, error) = await ValidarNombreAsync(request, null);
        if (error is not null)
            return error;

        var categoria = new CategoriaProducto
        {
            Nombre = nombre,
            Orden = await categoriasRepository.ObtenerSiguienteOrdenAsync(),
            Activa = true
        };

        categoriasRepository.Agregar(categoria);
        historialRepository.Agregar(HistorialCatalogoFactory.DeCategoria(categoria, idUsuario, HistorialCatalogoAcciones.Creado));

        if (!await GuardarAsync())
            return Result<CategoriaProductoResponse>.Conflict(NombreDuplicado);

        return new Result<CategoriaProductoResponse>(true,
            $"La categoría «{categoria.Nombre}» fue creada. Ya se puede elegir en los productos.", Mapear(categoria), 201);
    }

    public async Task<Result<CategoriaProductoResponse>> RenombrarAsync(int idCategoria, GuardarCategoriaProductoRequest request, int idUsuario)
    {
        var categoria = await categoriasRepository.ObtenerPorIdAsync(idCategoria, conProductos: true);
        if (categoria is null)
            return Result<CategoriaProductoResponse>.NotFound("La categoría no existe.");

        var (nombre, error) = await ValidarNombreAsync(request, idCategoria);
        if (error is not null)
            return error;

        if (categoria.Nombre == nombre)
            return Result<CategoriaProductoResponse>.Ok(Mapear(categoria), "No hay cambios que guardar.");

        historialRepository.Agregar(HistorialCatalogoFactory.DeCategoria(categoria, idUsuario, HistorialCatalogoAcciones.Editado,
            HistorialCatalogoCampos.Nombre, categoria.Nombre, nombre));
        categoria.Nombre = nombre;

        if (!await GuardarAsync())
            return Result<CategoriaProductoResponse>.Conflict(NombreDuplicado);

        return Result<CategoriaProductoResponse>.Ok(Mapear(categoria), "La categoría fue renombrada. Se refleja en todos sus productos.");
    }

    public async Task<Result<List<CategoriaProductoResponse>>> MoverAsync(int idCategoria, int direccion, int idUsuario)
    {
        var categorias = await categoriasRepository.ObtenerTodasParaReordenarAsync();
        var indice = categorias.FindIndex(c => c.IdCategoria == idCategoria);
        if (indice < 0)
            return Result<List<CategoriaProductoResponse>>.NotFound("La categoría no existe.");

        var destino = indice + (direccion < 0 ? -1 : 1);
        if (destino < 0)
            return Result<List<CategoriaProductoResponse>>.Conflict("La categoría ya está en la primera posición.");
        if (destino >= categorias.Count)
            return Result<List<CategoriaProductoResponse>>.Conflict("La categoría ya está en la última posición.");

        var movida = categorias[indice];
        (categorias[indice], categorias[destino]) = (categorias[destino], categorias[indice]);

        // Renormaliza 1..N: deja el orden consistente aunque hubiera empates.
        for (var i = 0; i < categorias.Count; i++)
            categorias[i].Orden = i + 1;

        historialRepository.Agregar(HistorialCatalogoFactory.DeCategoria(movida, idUsuario, HistorialCatalogoAcciones.Reordenado,
            HistorialCatalogoCampos.Posicion, (indice + 1).ToString(), (destino + 1).ToString()));
        await categoriasRepository.GuardarCambiosAsync();

        return await ObtenerTodasAsync();
    }

    public async Task<Result<CambioEstadoCategoriaResponse>> DeshabilitarAsync(int idCategoria, int idUsuario)
    {
        var categoria = await categoriasRepository.ObtenerPorIdAsync(idCategoria, conProductos: true);
        if (categoria is null)
            return Result<CambioEstadoCategoriaResponse>.NotFound("La categoría no existe.");

        if (!categoria.Activa)
            return Result<CambioEstadoCategoriaResponse>.Conflict("La categoría ya está deshabilitada.");

        var afectados = categoria.Productos.Where(p => p.Disponible).ToList();
        foreach (var producto in afectados)
        {
            producto.Disponible = false;
            historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Deshabilitado,
                motivo: $"{HistorialCatalogoMotivos.PorDeshabilitarCategoria}{categoria.Nombre}»"));
        }

        categoria.Activa = false;
        historialRepository.Agregar(HistorialCatalogoFactory.DeCategoria(categoria, idUsuario, HistorialCatalogoAcciones.Deshabilitado,
            motivo: afectados.Count == 0 ? null : $"{afectados.Count} {(afectados.Count == 1 ? "producto deshabilitado" : "productos deshabilitados")}"));

        await categoriasRepository.GuardarCambiosAsync();

        return Result<CambioEstadoCategoriaResponse>.Ok(
            new CambioEstadoCategoriaResponse { IdCategoria = categoria.IdCategoria, Activa = false, ProductosAfectados = afectados.Count },
            afectados.Count == 0
                ? $"La categoría «{categoria.Nombre}» fue deshabilitada."
                : $"La categoría «{categoria.Nombre}» fue deshabilitada junto con {afectados.Count} {(afectados.Count == 1 ? "producto" : "productos")}.");
    }

    public async Task<Result<CategoriaProductoResponse>> EliminarAsync(int idCategoria, int idUsuario)
    {
        var categoria = await categoriasRepository.ObtenerParaEliminarAsync(idCategoria);
        if (categoria is null)
            return Result<CategoriaProductoResponse>.NotFound("La categoría no existe.");

        // Los productos no se eliminan, asi que una categoria con productos (aunque esten deshabilitados) no se puede borrar.
        if (categoria.Productos.Count > 0)
            return Result<CategoriaProductoResponse>.Conflict(
                $"La categoría «{categoria.Nombre}» tiene {categoria.Productos.Count} {(categoria.Productos.Count == 1 ? "producto" : "productos")} y no se puede eliminar. Puedes deshabilitarla.");

        var respuesta = Mapear(categoria);

        // El historial queda con el nombre guardado; la FK pasa a NULL al borrar (ON DELETE SET NULL).
        foreach (var entrada in categoria.Historial)
            entrada.NombreElemento = categoria.Nombre;

        historialRepository.Agregar(new HistorialCatalogo
        {
            TipoElemento = HistorialCatalogoTipos.Categoria,
            NombreElemento = categoria.Nombre,
            IdUsuario = idUsuario,
            Fecha = DateTime.Now,
            Accion = HistorialCatalogoAcciones.Eliminado
        });

        categoriasRepository.Eliminar(categoria);
        await categoriasRepository.GuardarCambiosAsync();

        return Result<CategoriaProductoResponse>.Ok(respuesta, $"La categoría «{respuesta.Nombre}» fue eliminada.");
    }

    public async Task<Result<CambioEstadoCategoriaResponse>> HabilitarAsync(int idCategoria, int idUsuario)
    {
        var categoria = await categoriasRepository.ObtenerPorIdAsync(idCategoria, conProductos: true);
        if (categoria is null)
            return Result<CambioEstadoCategoriaResponse>.NotFound("La categoría no existe.");

        if (categoria.Activa)
            return Result<CambioEstadoCategoriaResponse>.Conflict("La categoría ya está habilitada.");

        var afectados = categoria.Productos.Where(p => !p.Disponible).ToList();
        foreach (var producto in afectados)
        {
            producto.Disponible = true;
            historialRepository.Agregar(HistorialCatalogoFactory.DeProducto(producto, idUsuario, HistorialCatalogoAcciones.Habilitado,
                motivo: $"{HistorialCatalogoMotivos.PorHabilitarCategoria}{categoria.Nombre}»"));
        }

        categoria.Activa = true;
        historialRepository.Agregar(HistorialCatalogoFactory.DeCategoria(categoria, idUsuario, HistorialCatalogoAcciones.Habilitado,
            motivo: afectados.Count == 0 ? null : $"{afectados.Count} {(afectados.Count == 1 ? "producto habilitado" : "productos habilitados")}"));

        await categoriasRepository.GuardarCambiosAsync();

        return Result<CambioEstadoCategoriaResponse>.Ok(
            new CambioEstadoCategoriaResponse { IdCategoria = categoria.IdCategoria, Activa = true, ProductosAfectados = afectados.Count },
            afectados.Count == 0
                ? $"La categoría «{categoria.Nombre}» fue habilitada."
                : $"La categoría «{categoria.Nombre}» fue habilitada junto con {afectados.Count} {(afectados.Count == 1 ? "producto" : "productos")}.");
    }

    // ------------------------------------------------------------
    // Soporte
    // ------------------------------------------------------------

    private async Task<(string Nombre, Result<CategoriaProductoResponse>? Error)> ValidarNombreAsync(
        GuardarCategoriaProductoRequest request, int? excluirIdCategoria)
    {
        var nombre = request.Nombre?.Trim() ?? "";

        if (nombre.Length == 0)
            return ("", Result<CategoriaProductoResponse>.BadRequest("El nombre de la categoría es obligatorio."));

        if (nombre.Length > 50)
            return ("", Result<CategoriaProductoResponse>.BadRequest("El nombre no puede superar los 50 caracteres."));

        if (await categoriasRepository.ExisteNombreAsync(nombre, excluirIdCategoria))
            return ("", Result<CategoriaProductoResponse>.Conflict(NombreDuplicado));

        return (nombre, null);
    }

    /// Guarda y devuelve false si otro administrador uso el mismo nombre un instante antes (indice unico).
    private async Task<bool> GuardarAsync()
    {
        try
        {
            await categoriasRepository.GuardarCambiosAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_CategoriaProducto_Nombre") == true)
        {
            return false;
        }
    }

    private static CategoriaProductoResponse Mapear(CategoriaProducto c) => new()
    {
        IdCategoria = c.IdCategoria,
        Nombre = c.Nombre,
        Orden = c.Orden,
        Activa = c.Activa,
        TotalProductos = c.Productos.Count,
        ProductosDisponibles = c.Productos.Count(p => p.Disponible)
    };
}
