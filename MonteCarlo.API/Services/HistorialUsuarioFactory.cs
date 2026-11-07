using MonteCarlo.API.Data.Entities;

namespace MonteCarlo.API.Services;

/// <summary>Arma las filas de auditoría de cuentas (HU-AUT-007). Nunca recibe contraseñas.</summary>
public static class HistorialUsuarioFactory
{
    public static HistorialUsuario Creada(Usuario afectado, int idUsuarioResponsable) =>
        Nueva(afectado, idUsuarioResponsable, HistorialUsuarioAcciones.Creada, null, null, null, null);

    public static HistorialUsuario Editada(
        Usuario afectado, int idUsuarioResponsable, string campo, string? anterior, string? nuevo) =>
        Nueva(afectado, idUsuarioResponsable, HistorialUsuarioAcciones.Editada, campo, anterior, nuevo, null);

    public static HistorialUsuario Estado(Usuario afectado, int idUsuarioResponsable, bool activo, string? motivo) =>
        Nueva(afectado, idUsuarioResponsable,
            activo ? HistorialUsuarioAcciones.Habilitada : HistorialUsuarioAcciones.Deshabilitada,
            HistorialUsuarioCampos.Activo, activo ? "No" : "Sí", activo ? "Sí" : "No", motivo);

    private static HistorialUsuario Nueva(
        Usuario afectado, int idUsuarioResponsable, string accion,
        string? campo, string? anterior, string? nuevo, string? motivo) => new()
    {
        // Navegación (no el id) para que el alta quede en el mismo SaveChanges que la cuenta nueva.
        UsuarioAfectado = afectado,
        NombreUsuarioAfectado = afectado.NombreCompleto,
        IdUsuario = idUsuarioResponsable,
        Accion = accion,
        Campo = campo,
        ValorAnterior = anterior,
        ValorNuevo = nuevo,
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
        Fecha = DateTime.Now
    };
}
