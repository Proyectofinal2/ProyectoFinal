using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;

namespace MonteCarlo.API.Data.Repositories;

public class HistorialUsuarioRepository(MonteCarloDbContext context) : IHistorialUsuarioRepository
{
    public async Task<(List<HistorialUsuario> Items, int Total)> BuscarAsync(
        string? busqueda, int? idUsuarioAfectado, int? idUsuario, string? accion, int pagina, int tamano)
    {
        var consulta = context.HistorialUsuarios.AsNoTracking()
            .Include(h => h.Usuario)
            .Include(h => h.UsuarioAfectado)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(h =>
                h.NombreUsuarioAfectado.Contains(texto) ||
                h.UsuarioAfectado.NombreUsuario.Contains(texto) ||
                h.UsuarioAfectado.CorreoElectronico.Contains(texto));
        }

        if (idUsuarioAfectado.HasValue)
            consulta = consulta.Where(h => h.IdUsuarioAfectado == idUsuarioAfectado.Value);

        if (idUsuario.HasValue)
            consulta = consulta.Where(h => h.IdUsuario == idUsuario.Value);

        if (!string.IsNullOrEmpty(accion))
            consulta = consulta.Where(h => h.Accion == accion);

        var total = await consulta.CountAsync();

        var items = await consulta
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.IdHistorialUsuario)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Usuario>> ObtenerResponsablesAsync()
    {
        // Solo id y nombre: no se carga la fila completa (con el hash de contraseña) para llenar un filtro.
        var responsables = await context.HistorialUsuarios.AsNoTracking()
            .Select(h => new { h.IdUsuario, h.Usuario.NombreCompleto }).Distinct()
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();

        return responsables
            .Select(u => new Usuario { IdUsuario = u.IdUsuario, NombreCompleto = u.NombreCompleto })
            .ToList();
    }
}
