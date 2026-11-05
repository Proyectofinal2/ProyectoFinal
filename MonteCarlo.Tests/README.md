# MonteCarlo.Tests

Guía de implementación y checkpoint de las pruebas automáticas. **Solo cubren lógica crítica o densa** (dinero, concurrencia, estados, reglas de la base de datos). No se prueba CRUD simple, mapeos ni vistas.

## Cómo correr

```powershell
dotnet test MonteCarlo.Tests                              # todo
dotnet test MonteCarlo.Tests --filter "HU=CON-002"        # una historia
dotnet test MonteCarlo.Tests --filter "Escenario=9"       # un criterio de aceptación
dotnet test MonteCarlo.Tests --filter "Hueco=G1"          # huecos conocidos
```

Si la API está corriendo y bloquea `MonteCarlo.API.exe`, agrega `-p:UseAppHost=false`. Si además bloquea `MonteCarlo.API.dll` (Visual Studio con la API en ejecución), compila a otra carpeta **dentro del repo** (las pruebas buscan `database.sql` subiendo desde la salida): `dotnet test MonteCarlo.Tests -p:UseAppHost=false -p:OutDir="$PWD/MonteCarlo.Tests/bin/alt/"` (`bin/` está ignorado por git).

## Base de datos temporal

- Las pruebas de integración usan **SQL Server real** (EF InMemory no aplica CHECK, índices filtrados ni `rowversion`).
- Cadena de conexión: variable de entorno `MONTECARLO_TEST_SQL` (sin `Initial Catalog`). Por defecto: `Data Source=DESKTOP-2KE6JBI\SQLEXPRESS;Integrated Security=True;Trust Server Certificate=True`.
- Cada corrida crea `MonteCarlo_test_<8 hex>` desde una **copia en memoria** de `database.sql` con el nombre cambiado (`Infra/ScriptsSql.cs`) y la elimina al terminar (`SqlServerFixture`). Aborta si el script adaptado aún nombra la base real, y solo elimina bases `MonteCarlo_test_*`.
- **Nunca** se ejecuta `database.sql` tal cual (borra tablas y usa `USE MonteCarlo`) ni se toca la base `MonteCarlo`.
- Sin SQL Server, los `[SqlFact]`/`[SqlTheory]` se **omiten con motivo** (no fallan).
- Verificar que no quedó nada: `SELECT name FROM sys.databases WHERE name LIKE 'MonteCarlo[_]test[_]%'` debe devolver 0 filas.

## Arquitectura del proyecto

```
MonteCarlo.Tests/
├── Infra/        SqlServerFixture (crea/elimina la base), ScriptsSql (adapta database.sql),
│                 PruebaSql (clase base), Atajos (armar pedidos)
├── Builders/     Mundo (datos propios por prueba: mesas, reservas, productos) y Sesion
│                 (servicios reales sobre un DbContext; dos Sesion = dos peticiones simultáneas)
├── Ventas/       cuenta, pedidos, quitar, cobrar, liberar sin consumo
├── Reservas/     bloqueos con cuenta abierta; mesas con cuenta abierta en creación, asignación y calendario;
│                 CuentaVencimiento (función pura, sin SQL)
├── Mesas/        estado en vivo + huecos conocidos
└── Datos/        restricciones de la base de datos
```

Reglas para escribir pruebas nuevas:

1. Hereda de `PruebaSql`, usa `[SqlFact]`/`[SqlTheory]` y `Mundo` para crear **datos propios** (números y nombres únicos); no dependas del orden ni de otras pruebas.
2. Servicios y repositorios **reales** sobre la base temporal; solo se simula `INotificacionesService` (NSubstitute).
3. Nombre en español que describa el escenario: `Cobrar_conTotalDesactualizado_devuelve409...`. Patrón Arrange-Act-Assert.
4. Cada prueba lleva `[Trait("HU", "CON-xxx")]` y `[Trait("Escenario", "n")]` (o `Regla` / `Hueco`).
5. Un caso por prueba; usa `Theory` solo si varía un dato. Antes de agregar una prueba pregunta: ¿protege dinero, concurrencia, un estado o una regla de la BD? Si no, no va.
6. Un defecto real que revele una prueba se **reporta**, no se oculta; un hueco conocido se fija con `Trait("Hueco", ...)` y se invierte cuando se implemente su plan.
7. Horas relativas a «ahora»: pueden fallar en los 10 min previos a la medianoche.

## Correlación tickets ↔ pruebas

| Ticket | Historia | Estado de las pruebas |
|---|---|---|
| #35 | HU-CON-002 cuenta de mesa y cobro | **Cubierto** (tabla siguiente) |
| #36 | HU-CON-003 venta para llevar | Pendiente: al implementar `para-llevar` agregar pruebas de total automático y de que exige al menos un producto |
| #35 | HU-CON-002 esc. 13 (cuenta antigua) y 14 (mesa con cuenta abierta no se ofrece) y HU-RES-003 (sin mesa no hay reserva) | **Cubierto** (tablas «Cuentas abiertas y disponibilidad» y «Cuenta antigua») |
| — | HU-CON-001 catálogo | Sin pruebas propias (CRUD); solo se cubre lo que usa la venta (producto disponible + categoría activa) |
| — | HU-CON-004 / HU-REP-003 | Pendiente: cuando existan, probar que solo `Estado = 'Cobrada'` cuenta como ingreso |

Al empezar un ticket nuevo: agrega su fila aquí, escribe primero las pruebas de sus escenarios críticos y márcalas con su `Trait("HU", ...)`.

### HU-CON-002 (#35): criterio → pruebas

Los criterios 10 (vistas Tarjetas/Plano) y 12 (Reservas del día, Paso 2) **no son automatizables aquí** o no están implementados: revisión manual.

| Criterio | Pruebas (clase → método) |
|---|---|
| 1 Registro y cierre exitoso | `CobrarTests`: `Cobrar_cuentaConPedidos_marcaCobradaFinalizaLaReservaYLiberaLaMesa`, `Cobrar_precioCongeladoAlAgregar...`, `Cobrar_reservaConDosMesasUnidas...` |
| 2 Cierre sin consumo | `LiberarSinConsumoTests`: `..._cuentaVacia_creaVentaCobradaEnCeroSinMetodoDePago`, `..._conProductosActivos_devuelve409...` |
| 3 Producto no disponible | `AgregarPedidoTests`: `..._productoDeshabilitado_devuelve409YLaCuentaNoCambia` |
| 4 Reserva no Confirmada | `AgregarPedidoTests`: `..._reservaNoConfirmada_devuelve409ConLaCuentaYNoCreaVenta`, `ReservaInexistente_devuelve404...` |
| 5 Agregar pedidos | `AgregarPedidoTests`: `primerPedido_abreLaCuenta...`, `segundoPedido_...`, `mismaClaveDosVeces_...` (idempotencia), `dosPrimerosPedidosSimultaneos_...` (concurrencia), `cantidadFueraDeRango_...` |
| 6 Corregir la cuenta | `QuitarProductoTests`: `lineaCompleta_...`, `quitarUnoDeTres_...`, `enCuentaCobrada_...` |
| 7 Estado de las mesas | `EstadoEnVivoTests` (4), `CuentaEstadoTests` (2) |
| 8 Reserva con consumo no se cancela | `BloqueoConCuentaAbiertaTests`: cancelar, No Show, cambio de estado (409 con cuenta abierta) |
| 9 Cuenta modificada durante el cobro | `CobrarTests`: `conTotalDesactualizado_...`, `versionVieja_conElMismoTotal_...`, `dosCobrosSimultaneos_...` |
| 11 Esperando llegada | `EstadoEnVivoTests`: `..._estaOcupadaEsperandoLlegada`; `LiberarSinConsumoTests`: `..._reservaSinLlegada_devuelve409...` |
| Reglas de la BD | `RestriccionesVentaTests`: `CK_Venta` (abierta con método de pago, cobrada con total sin método), `UQ_Venta_Reserva`, `FK_DetalleVenta_Pedido` + `Subtotal` calculado |

### Cuentas abiertas y disponibilidad (plan `HU-CON-002-cuentas-abiertas-vencidas.md`, fase A1)

Una mesa con cuenta abierta no se ofrece ni se asigna a una reserva que empieza antes de la **liberación estimada** (llegada del cliente + duración del turno, nunca antes de ahora); una posterior sí puede tomarla. El walk-in siempre la rechaza. Sin mesa no hay reserva (409, sin Pendiente ni umbral).

| Comportamiento | Pruebas |
|---|---|
| Walk-in sobre mesa con cuenta (409) y sugerencia sin esa mesa | `HuecosConocidosTests`: `G1_crearWalkInSobreMesaOcupadaPorCuentaAbierta_esConflicto`, `G1_sugerirWalkInNoOfreceLaMesaConCuentaAbierta` |
| `PUT mesas/{id}/liberar` encuentra la cuenta vacía de ayer (R1) | `HuecosConocidosTests.R1_liberarPorMesa...` |
| Asignar mesa dentro del turno (409) y posterior al turno (sí) | `CuentaAbiertaYDisponibilidadTests`: `AsignarMesa_aReservaDentroDelTurno...`, `AsignarMesa_aReservaPosteriorAlTurno...` |
| Liberación estimada: recién sentado bloquea; sentados hace casi un turno dejan tomar la mesa; el walk-in siempre bloquea | `AsignarMesa_recienSentado...`, `AsignarMesa_sentadosHaceCasiUnTurno...`, `CrearWalkIn_cuentaQueYaPasoElTurno...` |
| Crear reserva: sin mesa 409 y sin cliente huérfano; grupo grande confirmado; hoy sobre la única mesa con cuenta | `Crear_sinMesaDisponible...`, `Crear_grupoGrande_seConfirma...`, `Crear_hoyDentroDelTurno...` |
| Calendario de hoy no ofrece horarios antes de la liberación estimada; cuenta vencida no bloquea; mañana sí | `Calendario_hoy_noOfreceHorariosAntes...`, `Calendario_hoy_cuentaQueYaPasoElTurno...`, `Calendario_manana_...` |

Estas pruebas deshabilitan temporalmente las demás mesas de la base compartida y las restauran al terminar (`ConSoloLasMesasAsync`).

### Cuenta antigua (HU-CON-002 esc. 13, fase A2)

Una cuenta abierta es antigua (por cobrar) cuando pasó el tiempo de uso de la mesa (llegada + `DuracionReservaMinutos`) más `ToleranciaCobroMinutos` (30 por defecto, admite 0): con los valores por defecto, a las 2 h de haberse sentado.

| Comportamiento | Pruebas |
|---|---|
| Cálculo del vencimiento: llegada + turno + margen, otra duración, margen 0, cruza la medianoche, límite exacto, dentro del turno / del margen / pasado | `CuentaVencimientoTests` (funciones puras: no necesitan SQL Server) |
| Mesas y cuenta marcan `Antigua` / `VenceDesde` (dentro de la tolerancia no, pasada sí, con tolerancia 0 al terminar el turno); resumen cuenta antiguas; una cuenta cobrada no se marca | `CuentaAntiguaTests` |

`alter_database-6.sql` (clave `ToleranciaCobroMinutos`) se verificó a mano en una base desechable: aborta con mensaje sin `ConfiguracionSistema` y es idempotente (dos corridas, una fila).

### Huecos conocidos (fijan el comportamiento actual)

| Hueco | Prueba | Cuándo invertir la aserción |
|---|---|---|
| Cuenta vieja sin límite | `HuecosConocidosTests.CuentaAbiertaDeHaceDosDias...` | Mismo plan (vencimiento / `Abandonada`) |

G1 y R1 ya están corregidos (fase A1). Pendiente en el mismo plan: liberación estimada de la mesa (A2), antigüedad, «Cerrar sin cobro» y cierre automático. Hallazgos completos de la revisión: `.claude/plans/HU-CON-002-revision-codigo-1.md`.

## Checkpoint

- [x] Proyecto en `MonteCarlo.slnx`, base temporal segura, 66 pruebas verdes (HU-CON-002, cuentas antiguas y reservas con cuenta abierta).
- [x] Concurrencia: doble primer pedido y doble cobro.
- [ ] Equivalencia `alter_database-5.sql` (ejecutado dos veces) vs `database.sql` (columnas, CHECK, FK, índices).
- [ ] Autorización por rol de `VentasController` y `Mesas/liberar` (a nivel de atributos).
- [ ] Pruebas de JS puro (`mc-util.js`) con `node --test`.
- [ ] HU-CON-003 (#36) y reportes de ingresos (solo `Cobrada`).
