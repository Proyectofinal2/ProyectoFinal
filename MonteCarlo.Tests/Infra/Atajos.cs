using MonteCarlo.API.DTOs;

namespace MonteCarlo.Tests;

/// <summary>Atajos para armar pedidos y leer el 409 que lleva la cuenta actual.</summary>
public static class Atajos
{
    public static AgregarPedidoRequest Pedido(params (int IdProducto, int Cantidad)[] lineas) =>
        PedidoConClave(Guid.NewGuid(), lineas);

    public static AgregarPedidoRequest PedidoConClave(Guid clave, params (int IdProducto, int Cantidad)[] lineas) => new()
    {
        ClaveIdempotencia = clave,
        Lineas = lineas.Select(l => new LineaPedidoRequest { IdProducto = l.IdProducto, Cantidad = l.Cantidad }).ToList()
    };

    public static CuentaResponse? CuentaDelError<T>(Result<T> resultado) => resultado.ErrorData as CuentaResponse;
}
