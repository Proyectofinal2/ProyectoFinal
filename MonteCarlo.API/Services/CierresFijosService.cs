using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

public class CierresFijosService(ICierresFijosRepository cierresFijosRepository) : ICierresFijosService
{
    public async Task<bool> EsCierreFijoAsync(byte diaSemana)
    {
        var cierres = await cierresFijosRepository.ObtenerTodosAsync();
        return cierres.Any(c => c.DiaSemana == diaSemana);
    }

    public async Task<Result<CierresFijosResponse>> ObtenerAsync()
    {
        var actuales = await cierresFijosRepository.ObtenerTodosAsync();
        return Result<CierresFijosResponse>.Ok(Mapear(actuales), "Cierres fijos obtenidos exitosamente.");
    }

    public async Task<Result<CierresFijosResponse>> ActualizarAsync(IEnumerable<int> dias, int idUsuario)
    {
        var seleccion = dias.Distinct().ToList();

        if (seleccion.Any(d => d is < 0 or > 6))
        {
            return Result<CierresFijosResponse>.BadRequest("Los días deben estar entre 0 (domingo) y 6 (sábado).");
        }

        var actuales = await cierresFijosRepository.ObtenerTodosAsync();

        // Diferencia entre lo guardado y lo seleccionado: solo se toca lo que cambió.
        var aEliminar = actuales.Where(c => !seleccion.Contains(c.DiaSemana)).ToList();
        var aAgregar = seleccion
            .Where(d => actuales.All(c => c.DiaSemana != d))
            .Select(d => new CierreFijo { DiaSemana = (byte)d, IdUsuarioCreacion = idUsuario })
            .ToList();

        if (aEliminar.Count == 0 && aAgregar.Count == 0)
        {
            return Result<CierresFijosResponse>.Ok(Mapear(actuales), "No hubo cambios en los cierres fijos.");
        }

        // Auditoría (HU-CFG-004): una fila por día agregado o quitado.
        var historial = aAgregar
            .Select(c => NuevoHistorial("Registrado", c.DiaSemana, idUsuario))
            .Concat(aEliminar.Select(c => NuevoHistorial("Eliminado", c.DiaSemana, idUsuario)))
            .ToList();

        // No se tocan reservas ni cierres eventuales: el cierre fijo solo excluye días
        // del calendario a partir de ahora.
        await cierresFijosRepository.GuardarCambiosAsync(aAgregar, aEliminar, historial);

        var actualizados = await cierresFijosRepository.ObtenerTodosAsync();
        return Result<CierresFijosResponse>.Ok(Mapear(actualizados), "Los cierres fijos se actualizaron correctamente.");
    }

    private static HistorialCierre NuevoHistorial(string accion, byte dia, int idUsuario) => new()
    {
        TipoCierre = "Fijo",
        Accion = accion,
        DiaSemana = dia,
        IdUsuario = idUsuario,
        Fecha = DateTime.Now
    };

    private static CierresFijosResponse Mapear(IEnumerable<CierreFijo> cierres) =>
        new() { Dias = cierres.Select(c => (int)c.DiaSemana).OrderBy(d => d).ToList() };
}
