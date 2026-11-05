using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MonteCarlo.API.Data.Entities;
using MonteCarlo.API.Data.Repositories.Interfaces;
using MonteCarlo.API.DTOs;
using MonteCarlo.API.Services.Interfaces;

namespace MonteCarlo.API.Services;

/// <summary>
/// Cuenta abierta de la mesa y cobro (HU-CON-002). El precio y el total los decide el servidor: la pantalla solo
/// envia producto y cantidad, y el total que vio al cobrar. Solo las ventas Cobradas son ingreso.
/// </summary>
public class VentasService(
    IVentasRepository ventasRepository,
    IReservasRepository reservasRepository,
    IProductosRepository productosRepository,
    IMetodosPagoRepository metodosPagoRepository,
    IConfiguracionService configuracionService,
    ILogger<VentasService> logger) : IVentasService
{
    private const string Confirmada = "Confirmada";
    private const string Finalizada = "Finalizada";
    private const int MaxLineasPorPedido = 50;
    private const int MaxCantidadPorLinea = 99;
    private const string MotivoQuitadoPorDefecto = "Quitado de la cuenta";
    private const string ReservaNoEncontrada = "No se encontro la reserva.";
    private const string MotivoCuentaCerradaSinCobro = "Cuenta cerrada sin cobro";

    // ============================================================
    // Consulta
    // ============================================================

    public async Task<Result<CuentaResponse>> ObtenerCuentaAsync(int idReserva)
    {
        var (reserva, venta) = await CargarAsync(idReserva);
        if (reserva is null)
            return Result<CuentaResponse>.NotFound(ReservaNoEncontrada);

        var cuenta = ConstruirCuenta(reserva, venta);
        await AnotarVencimientoAsync(cuenta, reserva, venta);

        return Result<CuentaResponse>.Ok(cuenta, "Cuenta obtenida exitosamente.");
    }

    /// <summary>
    /// Marca si la cuenta abierta ya es antigua (ver <see cref="CuentaVencimiento"/>). Solo la consulta de la cuenta lo
    /// calcula: es lo que pide el panel y evita leer horarios y margen en cada cambio de la cuenta.
    /// </summary>
    private async Task AnotarVencimientoAsync(CuentaResponse cuenta, Reserva reserva, Venta? venta)
    {
        if (venta?.Estado != VentaEstados.Abierta)
            return;

        var duracionMinutos = await configuracionService.ObtenerValorDuracionReservaAsync();
        var toleranciaMinutos = await configuracionService.ObtenerValorToleranciaCobroAsync();
        var venceDesde = CuentaVencimiento.Calcular(reserva.FechaAtencion ?? venta.FechaVenta, duracionMinutos, toleranciaMinutos);

        cuenta.VenceDesde = venceDesde;
        cuenta.Antigua = DateTime.Now > venceDesde;
        cuenta.PuedeCerrarSinCobro = cuenta.Antigua && cuenta.PuedeCobrar;
    }

    public async Task<Result<List<MetodoPagoResponse>>> ObtenerMetodosPagoAsync(bool incluirInactivos = false)
    {
        var metodos = incluirInactivos
            ? await metodosPagoRepository.ObtenerTodosAsync()
            : await metodosPagoRepository.ObtenerActivosAsync();
        var respuesta = metodos
            .Select(m => new MetodoPagoResponse { IdMetodoPago = m.IdMetodoPago, Nombre = m.Nombre, Activo = m.Activo })
            .ToList();

        return Result<List<MetodoPagoResponse>>.Ok(respuesta, "Metodos de pago obtenidos exitosamente.");
    }

    public async Task<Result<List<CategoriaVentaResponse>>> ObtenerCatalogoAsync()
    {
        var productos = await productosRepository.ObtenerDisponiblesParaVentaAsync();
        var respuesta = productos
            .GroupBy(p => p.IdCategoria)
            .Select(g => (Categoria: g.First().Categoria, Productos: g))
            .OrderBy(g => g.Categoria.Orden).ThenBy(g => g.Categoria.IdCategoria)
            .Select(g => new CategoriaVentaResponse
            {
                IdCategoria = g.Categoria.IdCategoria,
                Nombre = g.Categoria.Nombre,
                Orden = g.Categoria.Orden,
                Productos = g.Productos
                    .OrderBy(p => p.Nombre)
                    .Select(p => new ProductoVentaResponse { IdProducto = p.IdProducto, Nombre = p.Nombre, Precio = p.Precio })
                    .ToList()
            })
            .ToList();

        return Result<List<CategoriaVentaResponse>>.Ok(respuesta, "Catalogo de venta obtenido exitosamente.");
    }

    // ============================================================
    // Pedidos
    // ============================================================

    public async Task<Result<CuentaResponse>> AgregarPedidoAsync(int idReserva, AgregarPedidoRequest request, int idUsuario)
    {
        if (request.ClaveIdempotencia == Guid.Empty)
            return Result<CuentaResponse>.BadRequest("Falta la clave del pedido.");

        var (reserva, venta) = await CargarAsync(idReserva);
        if (reserva is null)
            return Result<CuentaResponse>.NotFound(ReservaNoEncontrada);

        // Reintento o doble clic: el pedido ya esta registrado, se devuelve la cuenta tal cual.
        var existente = await ventasRepository.ObtenerPedidoPorClaveAsync(request.ClaveIdempotencia);
        if (existente is not null)
        {
            return existente.Venta.IdReserva == idReserva
                ? Result<CuentaResponse>.Ok(ConstruirCuenta(reserva, venta), "El pedido ya estaba registrado.")
                : Result<CuentaResponse>.Conflict("La clave del pedido ya se uso en otra cuenta. Intenta de nuevo.");
        }

        var cuentaActual = ConstruirCuenta(reserva, venta);
        if (!cuentaActual.PuedeAgregar)
            return ConflictoConCuenta<CuentaResponse>(cuentaActual.Bloqueo ?? "La cuenta no admite mas pedidos.", cuentaActual);

        var (lineas, errorLineas) = NormalizarLineas(request.Lineas, "el pedido");
        if (errorLineas is not null)
            return Result<CuentaResponse>.BadRequest(errorLineas);

        var catalogo = (await productosRepository.ObtenerDisponiblesParaVentaAsync(lineas.Select(l => l.IdProducto).ToList()))
            .ToDictionary(p => p.IdProducto);
        var noDisponible = lineas.FirstOrDefault(l => !catalogo.ContainsKey(l.IdProducto));
        if (noDisponible is not null)
            return ConflictoConCuenta<CuentaResponse>(
                "Uno de los productos ya no esta disponible. Actualiza el menu e intenta de nuevo.", cuentaActual);

        var ahora = DateTime.Now;
        var esNueva = venta is null;
        if (venta is null)
        {
            venta = new Venta
            {
                TipoVenta = VentaTipos.Consumo,
                Estado = VentaEstados.Abierta,
                IdReserva = idReserva,
                IdUsuarioRegistro = idUsuario,
                FechaVenta = ahora
            };
            ventasRepository.Agregar(venta);
        }

        var pedido = new PedidoVenta
        {
            Venta = venta,
            IdUsuario = idUsuario,
            Fecha = ahora,
            ClaveIdempotencia = request.ClaveIdempotencia
        };
        venta.Pedidos.Add(pedido);

        AgregarDetalles(venta, pedido, lineas, catalogo);

        RecalcularTotal(venta);
        if (!esNueva)
            ventasRepository.MarcarModificada(venta);

        // Respaldo: la interfaz usa "Cliente llegó", pero el primer pedido implica que el cliente ya esta sentado.
        reserva.FechaAtencion ??= ahora;

        var errorGuardado = await GuardarAsync<CuentaResponse>("La cuenta cambio mientras registrabas el pedido. Revisala e intenta de nuevo.");
        if (errorGuardado is not null)
            return errorGuardado;

        return await ObtenerCuentaTrasCambioAsync(idReserva, esNueva ? "Cuenta abierta con el primer pedido." : "Pedido registrado.");
    }

    // ============================================================
    // Venta para llevar (HU-CON-003)
    // ============================================================

    public async Task<Result<CobroResponse>> RegistrarParaLlevarAsync(ParaLlevarRequest request, int idUsuario)
    {
        if (request.ClaveIdempotencia == Guid.Empty)
            return Result<CobroResponse>.BadRequest("Falta la clave de la venta.");

        // Reintento o doble clic: la venta ya esta registrada, se devuelve tal cual.
        var existente = await ventasRepository.ObtenerPedidoPorClaveAsync(request.ClaveIdempotencia);
        if (existente is not null)
        {
            if (existente.Venta.IdReserva is not null)
                return Result<CobroResponse>.Conflict("La clave de la venta ya se uso en otra cuenta. Intenta de nuevo.");

            var metodoPrevio = existente.Venta.IdMetodoPago is { } id ? await metodosPagoRepository.ObtenerPorIdAsync(id) : null;
            return Result<CobroResponse>.Ok(MapearCobro(existente.Venta, null, metodoPrevio?.Nombre), "La venta ya estaba registrada.");
        }

        var (lineas, errorLineas) = NormalizarLineas(request.Lineas, "la venta");
        if (errorLineas is not null)
            return Result<CobroResponse>.BadRequest(errorLineas);

        var metodo = await metodosPagoRepository.ObtenerPorIdAsync(request.IdMetodoPago);
        if (metodo is null || !metodo.Activo)
            return Result<CobroResponse>.BadRequest("El metodo de pago no es valido.");

        var catalogo = (await productosRepository.ObtenerDisponiblesParaVentaAsync(lineas.Select(l => l.IdProducto).ToList()))
            .ToDictionary(p => p.IdProducto);
        if (lineas.Any(l => !catalogo.ContainsKey(l.IdProducto)))
            return Result<CobroResponse>.Conflict("Uno de los productos ya no esta disponible. Actualiza el menu e intenta de nuevo.");

        var total = lineas.Sum(l => l.Cantidad * catalogo[l.IdProducto].Precio);
        if (request.TotalEsperado != total)
            return Result<CobroResponse>.Conflict($"Los precios cambiaron: el total ahora es ₡{total:N0}. Actualiza el menu e intenta de nuevo.");

        var ahora = DateTime.Now;
        var venta = new Venta
        {
            TipoVenta = VentaTipos.Takeout,
            Estado = VentaEstados.Cobrada,
            IdUsuarioRegistro = idUsuario,
            FechaVenta = ahora,
            IdMetodoPago = metodo.IdMetodoPago,
            FechaCobro = ahora,
            IdUsuarioCobro = idUsuario
        };
        ventasRepository.Agregar(venta);

        // Los productos cuelgan de un pedido (FK compuesta): la venta para llevar tiene uno solo.
        var pedido = new PedidoVenta { Venta = venta, IdUsuario = idUsuario, Fecha = ahora, ClaveIdempotencia = request.ClaveIdempotencia };
        venta.Pedidos.Add(pedido);
        AgregarDetalles(venta, pedido, lineas, catalogo);
        RecalcularTotal(venta);

        var errorGuardado = await GuardarAsync<CobroResponse>("La venta no se pudo registrar. Intenta de nuevo.");
        if (errorGuardado is not null)
            return errorGuardado;

        return Result<CobroResponse>.Ok(MapearCobro(venta, null, metodo.Nombre), "Venta para llevar registrada.");
    }

    public async Task<Result<CuentaResponse>> AnularLineaAsync(int idDetalle, AnularLineaRequest request, int idUsuario)
    {
        var linea = await ventasRepository.ObtenerLineaAsync(idDetalle);
        if (linea is null)
            return Result<CuentaResponse>.NotFound("No se encontro la linea de la cuenta.");

        var venta = linea.Venta;
        if (venta.Estado != VentaEstados.Abierta || venta.IdReserva is null)
            return Result<CuentaResponse>.Conflict("Solo se pueden quitar productos de una cuenta abierta.");
        if (linea.Anulado)
            return Result<CuentaResponse>.Conflict("Ese producto ya fue quitado de la cuenta.");

        var cantidadQuitar = request.Cantidad ?? linea.Cantidad;
        if (cantidadQuitar < 1 || cantidadQuitar > linea.Cantidad)
            return Result<CuentaResponse>.BadRequest($"La cantidad a quitar debe estar entre 1 y {linea.Cantidad}.");

        var ahora = DateTime.Now;
        linea.Anulado = true;
        linea.MotivoAnulacion = string.IsNullOrWhiteSpace(request.Motivo) ? MotivoQuitadoPorDefecto : request.Motivo.Trim();
        linea.FechaAnulacion = ahora;
        linea.IdUsuarioAnulacion = idUsuario;

        // "Quitar 1" de 3: se anula la linea completa y queda una de reemplazo con el resto, en el mismo pedido.
        if (cantidadQuitar < linea.Cantidad)
        {
            venta.Detalles.Add(new DetalleVenta
            {
                IdVenta = venta.IdVenta,
                IdPedido = linea.IdPedido,
                IdProducto = linea.IdProducto,
                Cantidad = linea.Cantidad - cantidadQuitar,
                PrecioUnitario = linea.PrecioUnitario
            });
        }

        RecalcularTotal(venta);
        ventasRepository.MarcarModificada(venta);

        var errorGuardado = await GuardarAsync<CuentaResponse>("La cuenta cambio mientras quitabas el producto. Revisala e intenta de nuevo.");
        if (errorGuardado is not null)
            return errorGuardado;

        return await ObtenerCuentaTrasCambioAsync(venta.IdReserva.Value, "Producto quitado de la cuenta.");
    }

    // ============================================================
    // Cobro
    // ============================================================

    public async Task<Result<CobroResponse>> CobrarAsync(int idReserva, CobrarRequest request, int idUsuario)
    {
        var (reserva, venta) = await CargarAsync(idReserva);
        if (reserva is null)
            return Result<CobroResponse>.NotFound(ReservaNoEncontrada);

        var cuenta = ConstruirCuenta(reserva, venta);

        if (venta is null)
            return ConflictoConCuenta<CobroResponse>("La cuenta no tiene pedidos. Si no hubo consumo, libera la mesa sin consumo.", cuenta);
        if (!cuenta.PuedeCobrar)
            return ConflictoConCuenta<CobroResponse>(
                cuenta.Bloqueo ?? "La cuenta no tiene productos. Si no hubo consumo, libera la mesa sin consumo.", cuenta);

        var errorVista = ValidarCuentaVista<CobroResponse>(venta, request.Version, request.TotalEsperado, cuenta);
        if (errorVista is not null)
            return errorVista;

        var metodo = await metodosPagoRepository.ObtenerPorIdAsync(request.IdMetodoPago);
        if (metodo is null || !metodo.Activo)
            return Result<CobroResponse>.BadRequest("El metodo de pago no es valido.");

        var errorEstado = await FinalizarReservaAsync<CobroResponse>(reserva);
        if (errorEstado is not null)
            return errorEstado;

        var ahora = DateTime.Now;
        venta.Estado = VentaEstados.Cobrada;
        venta.IdMetodoPago = metodo.IdMetodoPago;
        venta.FechaCobro = ahora;
        venta.IdUsuarioCobro = idUsuario;
        ventasRepository.MarcarModificada(venta);

        reserva.FechaAtencion ??= ahora;

        var errorGuardado = await GuardarAsync<CobroResponse>("La cuenta cambio mientras se cobraba. Revisala e intenta de nuevo.");
        if (errorGuardado is not null)
            return errorGuardado;

        return Result<CobroResponse>.Ok(MapearCobro(venta, reserva, metodo.Nombre), "Cuenta cobrada. La mesa quedo libre.");
    }

    public async Task<Result<CobroResponse>> CerrarSinCobroAsync(int idReserva, CerrarSinCobroRequest request, int idUsuario)
    {
        var motivo = request.Motivo?.Trim();
        if (string.IsNullOrEmpty(motivo) || motivo.Length < 3)
            return Result<CobroResponse>.BadRequest("El motivo debe tener entre 3 y 300 caracteres.");

        var (reserva, venta) = await CargarAsync(idReserva);
        if (reserva is null)
            return Result<CobroResponse>.NotFound(ReservaNoEncontrada);

        var cuenta = ConstruirCuenta(reserva, venta);
        await AnotarVencimientoAsync(cuenta, reserva, venta);

        if (venta is null || !cuenta.PuedeCobrar)
            return ConflictoConCuenta<CobroResponse>(
                cuenta.Bloqueo ?? "La cuenta no tiene productos. Si no hubo consumo, libera la mesa sin consumo.", cuenta);
        if (!cuenta.PuedeCerrarSinCobro)
            return ConflictoConCuenta<CobroResponse>(
                "La cuenta todavia no esta vencida: cobrala o espera a que pase el tiempo de uso de la mesa.", cuenta);

        var errorVista = ValidarCuentaVista<CobroResponse>(venta, request.Version, null, cuenta);
        if (errorVista is not null)
            return errorVista;

        var errorEstado = await FinalizarReservaAsync<CobroResponse>(reserva);
        if (errorEstado is not null)
            return errorEstado;

        // Se conservan el total y las lineas: queda registrado cuanto se consumio sin cobrar.
        ventasRepository.MarcarModificada(venta);
        venta.Estado = VentaEstados.Abandonada;
        venta.MotivoAnulacion = motivo;
        venta.FechaAnulacion = DateTime.Now;
        venta.IdUsuarioAnulacion = idUsuario;

        reserva.MotivoCancelacion = MotivoCuentaCerradaSinCobro;

        var errorGuardado = await GuardarAsync<CobroResponse>("La cuenta cambio mientras se cerraba. Revisala e intenta de nuevo.");
        if (errorGuardado is not null)
            return errorGuardado;

        return Result<CobroResponse>.Ok(MapearCobro(venta, reserva, null), "Cuenta cerrada sin cobro. La mesa quedo libre.");
    }

    public async Task<Result<CobroResponse>> LiberarSinConsumoAsync(int idReserva, int idUsuario)
    {
        var (reserva, venta) = await CargarAsync(idReserva);
        if (reserva is null)
            return Result<CobroResponse>.NotFound(ReservaNoEncontrada);

        var cuenta = ConstruirCuenta(reserva, venta);

        if (!cuenta.PuedeLiberarSinConsumo)
        {
            var motivo = cuenta.Bloqueo
                ?? (cuenta.TotalProductos > 0
                    ? "La cuenta tiene productos: cobrala en lugar de liberar la mesa."
                    : "El cliente todavia no ha llegado: marca su llegada o registra el No Show.");
            return ConflictoConCuenta<CobroResponse>(motivo, cuenta);
        }

        var errorEstado = await FinalizarReservaAsync<CobroResponse>(reserva);
        if (errorEstado is not null)
            return errorEstado;

        // Sin consumo no hay venta: una cuenta que nunca tuvo pedidos solo finaliza la reserva y libera la mesa.
        // Si ya habia una cuenta abierta (con todos sus productos quitados), se cierra en cero para conservar su historial.
        if (venta is not null)
        {
            var ahora = DateTime.Now;
            ventasRepository.MarcarModificada(venta);
            venta.Estado = VentaEstados.Cobrada;
            venta.Total = 0;
            venta.IdMetodoPago = null;
            venta.FechaCobro = ahora;
            venta.IdUsuarioCobro = idUsuario;
        }

        else if (await ventasRepository.ExisteParaReservaAsync(idReserva))
        {
            // Un pedido entro mientras se liberaba la mesa (la reserva no tiene token de concurrencia).
            return Result<CobroResponse>.Conflict("La cuenta cambio mientras se liberaba la mesa. Revisala e intenta de nuevo.");
        }

        reserva.MotivoCancelacion = "Liberada sin consumo";

        var errorGuardado = await GuardarAsync<CobroResponse>("La cuenta cambio mientras se liberaba la mesa. Revisala e intenta de nuevo.");
        if (errorGuardado is not null)
            return errorGuardado;

        return Result<CobroResponse>.Ok(MapearCobro(venta, reserva, null), "Mesa liberada sin consumo.");
    }

    /// <summary>
    /// Libera la mesa de una reserva ya sentada hoy: cierre sin consumo, que solo procede con la cuenta vacia.
    /// 400 si la mesa no tiene una reserva sentada.
    /// </summary>
    public async Task<Result<CobroResponse>> LiberarSinConsumoPorMesaAsync(int idMesa, int idUsuario)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var reserva = await reservasRepository.ObtenerConfirmadaActivaPorMesaAsync(idMesa, hoy);
        if (reserva is null)
            return Result<CobroResponse>.BadRequest("Esa mesa no tiene ninguna reserva activa que liberar.");

        return await LiberarSinConsumoAsync(reserva.IdReserva, idUsuario);
    }

    // ============================================================
    // Auxiliares
    // ============================================================

    /// <summary>La reserva y su cuenta (la cuenta es null mientras no hay pedidos; la reserva, si no existe).</summary>
    private async Task<(Reserva? Reserva, Venta? Venta)> CargarAsync(int idReserva)
    {
        var reserva = await reservasRepository.ObtenerParaCuentaAsync(idReserva);
        var venta = reserva is null ? null : await ventasRepository.ObtenerPorReservaAsync(idReserva);
        return (reserva, venta);
    }

    /// <summary>
    /// Compara lo que vio la pantalla con la cuenta actual: la version (RowVersion) y, si se envia, el total (dos cambios
    /// que se compensan dejan el mismo total pero cambian la version). 400 si la version no es valida; 409 con la cuenta
    /// actual si cambio; null si coincide.
    /// </summary>
    private static Result<T>? ValidarCuentaVista<T>(Venta venta, string? version, decimal? totalEsperado, CuentaResponse cuenta)
    {
        byte[] versionVista;
        try { versionVista = Convert.FromBase64String(version ?? ""); }
        catch (FormatException) { return Result<T>.BadRequest("La version de la cuenta no es valida."); }

        if ((totalEsperado is not null && totalEsperado != venta.Total) || !versionVista.AsSpan().SequenceEqual(venta.RowVersion))
            return ConflictoConCuenta<T>($"La cuenta cambio: el total ahora es ₡{venta.Total:N0}. Revisala antes de continuar.", cuenta);

        return null;
    }

    /// <summary>Pasa la reserva a Finalizada (la mesa queda libre). Devuelve null si salio bien, o el error si falta el estado.</summary>
    private async Task<Result<T>?> FinalizarReservaAsync<T>(Reserva reserva)
    {
        var estadoFinalizada = await reservasRepository.ObtenerEstadoAsync(Finalizada);
        if (estadoFinalizada is null)
            return Result<T>.InternalError("El estado \"Finalizada\" no esta configurado.");

        reserva.EstadoReserva = estadoFinalizada;
        return null;
    }

    private static List<int> NumerosMesa(Reserva reserva) =>
        reserva.ReservaMesas.Select(rm => rm.Mesa.NumeroMesa).OrderBy(n => n).ToList();

    /// <summary>
    /// Guarda los cambios. Devuelve null si salio bien; 409 si la cuenta cambio al mismo tiempo (concurrencia o indice
    /// unico: segunda cuenta de la reserva, clave de pedido repetida); 500 (y queda en el log) ante cualquier otro
    /// fallo de la base, para no disfrazar un error del sistema de conflicto.
    /// </summary>
    private async Task<Result<T>?> GuardarAsync<T>(string mensajeConflicto)
    {
        try
        {
            await ventasRepository.GuardarCambiosAsync();
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<T>.Conflict(mensajeConflicto);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result<T>.Conflict(mensajeConflicto);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "No se pudo guardar la cuenta.");
            return Result<T>.InternalError("No se pudo guardar la cuenta. Intenta de nuevo.");
        }
    }

    /// <summary>Agrega a la venta una linea por producto del pedido, con el precio vigente del catalogo (se congela aqui).</summary>
    private static void AgregarDetalles(Venta venta, PedidoVenta pedido, List<LineaPedidoRequest> lineas, Dictionary<int, Producto> catalogo)
    {
        foreach (var linea in lineas)
        {
            venta.Detalles.Add(new DetalleVenta
            {
                Venta = venta,
                Pedido = pedido,
                IdProducto = linea.IdProducto,
                Cantidad = linea.Cantidad,
                PrecioUnitario = catalogo[linea.IdProducto].Precio
            });
        }
    }

    /// <summary>
    /// Une las lineas repetidas del mismo producto y valida cuantas hay y sus cantidades.
    /// <paramref name="destino"/> completa el mensaje de "al menos un producto" ("el pedido", "la venta").
    /// </summary>
    private static (List<LineaPedidoRequest> Lineas, string? Error) NormalizarLineas(List<LineaPedidoRequest>? solicitadas, string destino)
    {
        var lineas = (solicitadas ?? [])
            .GroupBy(l => l.IdProducto)
            .Select(g => new LineaPedidoRequest { IdProducto = g.Key, Cantidad = g.Sum(l => l.Cantidad) })
            .ToList();

        var error = lineas.Count switch
        {
            0 => $"Agrega al menos un producto para registrar {destino}.",
            > MaxLineasPorPedido => $"Un pedido admite hasta {MaxLineasPorPedido} productos distintos.",
            _ when lineas.Any(l => l.Cantidad is < 1 or > MaxCantidadPorLinea) =>
                $"La cantidad de cada producto debe estar entre 1 y {MaxCantidadPorLinea}.",
            _ => null
        };
        return (lineas, error);
    }

    private async Task<Result<CuentaResponse>> ObtenerCuentaTrasCambioAsync(int idReserva, string mensaje)
    {
        var (reserva, venta) = await CargarAsync(idReserva);

        return Result<CuentaResponse>.Ok(ConstruirCuenta(reserva!, venta), mensaje);
    }

    /// <summary>El total siempre se recalcula desde las lineas activas, nunca se incrementa.</summary>
    private static void RecalcularTotal(Venta venta) =>
        venta.Total = venta.Detalles.Where(d => !d.Anulado).Sum(d => d.Cantidad * d.PrecioUnitario);

    /// <summary>Folio legible de una venta (V-000123); unico formato en listados, cuentas y Reservas.</summary>
    internal static string Folio(int idVenta) => $"V-{idVenta:D6}";

    /// <summary>409 que lleva la cuenta actual, para que la pantalla se refresque sin otra llamada.</summary>
    private static Result<T> ConflictoConCuenta<T>(string mensaje, CuentaResponse cuenta) =>
        new(false, mensaje, default, 409, cuenta);

    private static CobroResponse MapearCobro(Venta? venta, Reserva? reserva, string? metodoPago) => new()
    {
        IdVenta = venta?.IdVenta,
        Folio = venta is null ? null : Folio(venta.IdVenta),
        IdReserva = reserva?.IdReserva,
        NumerosMesa = reserva is null ? [] : NumerosMesa(reserva),
        Total = venta?.Total ?? 0,
        MetodoPago = metodoPago
    };

    private static CuentaResponse ConstruirCuenta(Reserva reserva, Venta? venta)
    {
        var activas = venta?.Detalles.Where(d => !d.Anulado).ToList() ?? [];
        var estadoReserva = reserva.EstadoReserva.Nombre;
        var bloqueo = CalcularBloqueo(reserva, venta);

        var puedeAgregar = bloqueo is null;
        var cuenta = new CuentaResponse
        {
            IdReserva = reserva.IdReserva,
            IdVenta = venta?.IdVenta,
            Folio = venta is null ? null : Folio(venta.IdVenta),
            Estado = venta?.Estado ?? "SinPedidos",
            EstadoReserva = estadoReserva,
            Cliente = reserva.NombreParaMostrar(),
            Personas = reserva.CantidadPersonas,
            NumerosMesa = NumerosMesa(reserva),
            FechaAtencion = reserva.FechaAtencion,
            Total = venta?.Total ?? 0,
            TotalProductos = activas.Sum(d => d.Cantidad),
            Version = venta?.Estado == VentaEstados.Abierta ? Convert.ToBase64String(venta.RowVersion) : null,
            PuedeAgregar = puedeAgregar,
            PuedeCobrar = puedeAgregar && venta is not null && activas.Count > 0,
            PuedeLiberarSinConsumo = puedeAgregar && reserva.FechaAtencion is not null && activas.Count == 0,
            Bloqueo = bloqueo
        };

        if (venta is null)
            return cuenta;

        cuenta.Pedidos = MapearPedidos(venta);
        return cuenta;
    }

    /// <summary>Por que no se puede registrar consumo en la reserva (nulo si se puede).</summary>
    private static string? CalcularBloqueo(Reserva reserva, Venta? venta)
    {
        var estadoReserva = reserva.EstadoReserva.Nombre;

        if (venta?.Estado == VentaEstados.Cobrada)
            return "La cuenta ya fue cobrada.";
        if (venta?.Estado == VentaEstados.Anulada)
            return "La cuenta fue anulada.";
        if (venta?.Estado == VentaEstados.Abandonada)
            return $"La cuenta fue cerrada sin cobro: {venta.MotivoAnulacion}";
        if (estadoReserva != Confirmada)
            return $"La reserva esta en estado \"{estadoReserva}\": no se puede registrar consumo.";
        if (reserva.ReservaMesas.Count == 0)
            return "La reserva no tiene mesa asignada.";
        if (reserva.FechaReserva > DateOnly.FromDateTime(DateTime.Now))
            return "La reserva es de una fecha futura: todavia no se puede registrar consumo.";
        return null;
    }

    private static List<PedidoCuentaResponse> MapearPedidos(Venta venta)
    {
        var lineasPorPedido = venta.Detalles.ToLookup(d => d.IdPedido);
        var numero = 0;

        return venta.Pedidos
            .OrderBy(p => p.Fecha).ThenBy(p => p.IdPedido)
            .Select(pedido =>
            {
                var lineas = lineasPorPedido[pedido.IdPedido]
                    .OrderBy(d => d.IdDetalleVenta)
                    .Select(d => new LineaCuentaResponse
                    {
                        IdDetalle = d.IdDetalleVenta,
                        IdProducto = d.IdProducto,
                        Producto = d.Producto.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario,
                        Anulado = d.Anulado
                    })
                    .ToList();

                return new PedidoCuentaResponse
                {
                    IdPedido = pedido.IdPedido,
                    Numero = ++numero,
                    Fecha = pedido.Fecha,
                    Usuario = pedido.Usuario.NombreCompleto,
                    Subtotal = lineas.Where(l => !l.Anulado).Sum(l => l.Subtotal),
                    Lineas = lineas
                };
            })
            .ToList();
    }

    public async Task<Result<List<VentaListadoResponse>>> ObtenerListadoAsync(FiltroVentasRequest filtro)
    {
        var ventas = await ventasRepository.ObtenerConFiltrosAsync(filtro);

        var respuesta = ventas.Select(v => new VentaListadoResponse
        {
            IdVenta = v.IdVenta,
            Folio = Folio(v.IdVenta),
            FechaVenta = v.FechaVenta,
            TipoVenta = v.TipoVenta,
            Total = v.Total,
            MetodoPago = v.MetodoPago?.Nombre,
            MetodoPagoActivo = v.MetodoPago?.Activo,
            Estado = v.Estado
        }).ToList();

        return Result<List<VentaListadoResponse>>.Ok(respuesta, $"{respuesta.Count} venta(s) encontrada(s).");
    }

    public async Task<Result<VentaDetalleResponse>> ObtenerDetalleAsync(int idVenta)
    {
        var venta = await ventasRepository.ObtenerDetallePorIdAsync(idVenta);
        if (venta is null) return Result<VentaDetalleResponse>.NotFound("Venta no encontrada.");

        var response = new VentaDetalleResponse
        {
            IdVenta = venta.IdVenta,
            Folio = Folio(venta.IdVenta),
            FechaVenta = venta.FechaVenta,
            TipoVenta = venta.TipoVenta,
            Estado = venta.Estado,
            Motivo = venta.MotivoAnulacion,
            Total = venta.Total,
            MetodoPago = venta.MetodoPago?.Nombre,
            MetodoPagoActivo = venta.MetodoPago?.Activo,
            UsuarioRegistro = venta.UsuarioRegistro.NombreCompleto,
            Lineas = venta.Detalles.Select(d => new LineaDetalleResponse
            {
                Producto = d.Producto.Nombre,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Subtotal
            }).ToList()
        };

        return Result<VentaDetalleResponse>.Ok(response, "Detalle obtenido exitosamente.");
    }
}
