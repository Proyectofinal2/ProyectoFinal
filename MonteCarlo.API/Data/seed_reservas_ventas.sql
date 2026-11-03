USE MonteCarlo;
GO

/* ============================================================
   seed_reservas_ventas.sql - Datos de prueba de reservas y ventas

   Para REVISAR las pantallas de Mesas, Reservas del dia, Ventas e
   Inicio con datos realistas. NO ejecutar en produccion.

   Requisitos: database.sql + alter_database-3.sql y
   alter_database-5.sql (cuenta de mesa y ventas). Si falta algo,
   el script se detiene sin cambiar nada.

   Que siembra (todo marcado con [SEED] para poder borrarlo):
   - Dias PASADOS (los ultimos 30 dias) y FUTUROS (hasta el fin del
     mes siguiente). Con la fecha de hoy = 31/oct: octubre completo
     y todo noviembre.
   - Solo en dias en que el restaurante ABRE, segun la base de datos:
     HorarioOperacion activo, sin CierreFijo (dia de la semana) y sin
     CierreEventual (fecha). Las horas de llegada son las del
     calendario de disponibilidad (apertura, cada intervalo, hasta
     que cabe la duracion de la reserva) y ninguna mesa se
     sobrepone con otra reserva (incluidas las que ya existian).
   - Reservas: Confirmada (futuras), Finalizada, No Show, Cancelada
     por el cliente, Cancelada por el administrador, Cancelada por
     cierre administrativo, walk-ins, grupos que usan 2 mesas
     adyacentes y observaciones.
   - Ventas: Cobrada (efectivo, tarjeta y SINPE; 1 a 3 pedidos),
     Cobrada sin consumo (liberada), Anulada (con motivo), lineas
     anuladas, ventas para llevar, y precios historicos distintos
     a los actuales en la primera mitad del periodo.
   - HOY (escenario de revision, ver @IncluirHoy): reservas y
     cuentas alrededor de la hora actual para ver los estados de
     Mesas: libre, reserva proxima, esperando llegada, ocupada sin
     pedido, ocupada con cuenta, cuenta que excede el turno, mesa
     unida, cobradas, anulada, sin consumo, No Show y cancelada.
     IMPORTANTE: estas filas IGNORAN el horario y los cierres (si
     hoy el restaurante esta cerrado, igual se siembran) porque su
     proposito es poder verificar las pantallas ahora mismo.
   - No siembra reservas Pendientes (el umbral de aprobacion ya no
     aplica) ni notificaciones.

   Es idempotente: cada ejecucion borra lo sembrado antes y lo
   vuelve a sembrar con las fechas de hoy. Para solo borrar, cambia
   @value de SeedAccion a LIMPIAR.
   ============================================================ */

EXEC sp_set_session_context @key = N'SeedAccion', @value = N'SEMBRAR';  -- SEMBRAR (limpia y siembra) o LIMPIAR (solo borra)
EXEC sp_set_session_context @key = N'SeedIncluirHoy', @value = 1;       -- 1 = incluir el escenario de HOY (ignora horario y cierres); 0 = no
GO

/* ------------------------------------------------------------
   0. Requisitos. Si faltan, se omite el resto del script.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.PedidoVenta', 'U') IS NULL OR COL_LENGTH('dbo.Venta', 'Estado') IS NULL
   OR OBJECT_ID('dbo.Mesa', 'U') IS NULL OR OBJECT_ID('dbo.HorarioOperacion', 'U') IS NULL
   OR OBJECT_ID('dbo.CategoriaProducto', 'U') IS NULL
BEGIN
    RAISERROR(N'Faltan tablas: ejecuta database.sql y los alter_database-3.sql y alter_database-5.sql antes de sembrar. No se aplico ningun cambio.', 16, 1);
    SET NOEXEC ON;
END
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Accion NVARCHAR(10) = CONVERT(NVARCHAR(10), ISNULL(SESSION_CONTEXT(N'SeedAccion'), N'SEMBRAR'));
DECLARE @IncluirHoy BIT = CONVERT(BIT, ISNULL(SESSION_CONTEXT(N'SeedIncluirHoy'), 1));

BEGIN TRY
BEGIN TRANSACTION;

/* ============================================================
   1. LIMPIEZA de lo sembrado antes
   ============================================================ */
CREATE TABLE #RViejas (IdReserva INT PRIMARY KEY);
INSERT #RViejas SELECT IdReserva FROM dbo.Reserva WHERE LEFT(Observaciones, 6) = N'[SEED]';

CREATE TABLE #VViejas (IdVenta INT PRIMARY KEY);
INSERT #VViejas
SELECT IdVenta FROM dbo.Venta WHERE IdReserva IN (SELECT IdReserva FROM #RViejas)
UNION
SELECT IdVenta FROM dbo.PedidoVenta WHERE LEFT(Nota, 6) = N'[SEED]';

DELETE FROM dbo.DetalleVenta WHERE IdVenta IN (SELECT IdVenta FROM #VViejas);
DELETE FROM dbo.PedidoVenta WHERE IdVenta IN (SELECT IdVenta FROM #VViejas);
DELETE FROM dbo.Venta WHERE IdVenta IN (SELECT IdVenta FROM #VViejas);
DELETE FROM dbo.Notificacion WHERE IdReserva IN (SELECT IdReserva FROM #RViejas);
DELETE FROM dbo.ReservaMesa WHERE IdReserva IN (SELECT IdReserva FROM #RViejas);
DELETE FROM dbo.Reserva WHERE IdReserva IN (SELECT IdReserva FROM #RViejas);
DELETE c FROM dbo.Cliente c
WHERE c.CorreoElectronico LIKE N'%@seed.montecarlo.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.Reserva r WHERE r.IdCliente = c.IdCliente);

DECLARE @cLimR INT, @cLimV INT;
SELECT @cLimR = COUNT(*) FROM #RViejas;
SELECT @cLimV = COUNT(*) FROM #VViejas;
PRINT 'Limpieza: ' + CAST(@cLimR AS VARCHAR(10)) + ' reservas y ' + CAST(@cLimV AS VARCHAR(10)) + ' ventas sembradas anteriormente eliminadas.';

IF @Accion = N'LIMPIAR'
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Accion LIMPIAR: no se sembro nada.';
    RETURN;
END

/* ============================================================
   2. CONTEXTO: configuracion, calendario, mesas, catalogo
   ============================================================ */
DECLARE @Ahora DATETIME2(0) = SYSDATETIME();
DECLARE @Hoy DATE = CAST(@Ahora AS DATE);
DECLARE @Desde DATE = DATEADD(DAY, -30, @Hoy);
DECLARE @Hasta DATE = EOMONTH(@Hoy, 1);
DECLARE @Duracion INT = ISNULL((SELECT TRY_CAST(Valor AS INT) FROM dbo.ConfiguracionSistema WHERE Clave = 'DuracionReservaMinutos'), 90);
DECLARE @Intervalo INT = ISNULL((SELECT TRY_CAST(Valor AS INT) FROM dbo.ConfiguracionSistema WHERE Clave = 'IntervaloCalendarioDisponibilidadMinutos'), 30);
IF @Duracion < 30 SET @Duracion = 90;
IF @Intervalo < 5 SET @Intervalo = 30;
DECLARE @Cambio DATE = DATEADD(DAY, -15, @Hoy);   -- antes de esta fecha las ventas usan el precio historico (95 %)

DECLARE @Walkin INT = (SELECT TOP 1 IdCliente FROM dbo.Cliente WHERE Nombre = N'Walk-in' AND Telefono = N'00000000' ORDER BY IdCliente);
IF @Walkin IS NULL THROW 50001, N'No existe el cliente centinela Walk-in (database.sql).', 1;

DECLARE @EstConf INT = (SELECT IdEstadoReserva FROM dbo.EstadoReserva WHERE Nombre = N'Confirmada');
DECLARE @EstCanc INT = (SELECT IdEstadoReserva FROM dbo.EstadoReserva WHERE Nombre = N'Cancelada');
DECLARE @EstFin  INT = (SELECT IdEstadoReserva FROM dbo.EstadoReserva WHERE Nombre = N'Finalizada');
DECLARE @EstNoSh INT = (SELECT IdEstadoReserva FROM dbo.EstadoReserva WHERE Nombre = N'No Show');

-- Usuarios activos (quien registra y cobra) y generales (quien anula)
CREATE TABLE #Usu (rn INT PRIMARY KEY, IdUsuario INT, Rol NVARCHAR(20));
INSERT #Usu SELECT ROW_NUMBER() OVER (ORDER BY IdUsuario), IdUsuario, Rol FROM dbo.Usuario WHERE Activo = 1;
DECLARE @nUsu INT = (SELECT COUNT(*) FROM #Usu);
IF @nUsu = 0 THROW 50002, N'No hay usuarios activos.', 1;
CREATE TABLE #UsuG (rn INT PRIMARY KEY, IdUsuario INT);
INSERT #UsuG SELECT ROW_NUMBER() OVER (ORDER BY IdUsuario), IdUsuario FROM dbo.Usuario WHERE Activo = 1 AND Rol = N'General';
DECLARE @nUsuG INT = (SELECT COUNT(*) FROM #UsuG);
IF @nUsuG = 0 THROW 50003, N'No hay usuarios activos con rol General (quien anula).', 1;

-- Metodos de pago activos
CREATE TABLE #Met (rn INT PRIMARY KEY, IdMetodoPago INT);
INSERT #Met SELECT ROW_NUMBER() OVER (ORDER BY IdMetodoPago), IdMetodoPago FROM dbo.MetodoPago WHERE Activo = 1;
DECLARE @nMet INT = (SELECT COUNT(*) FROM #Met);
IF @nMet = 0 THROW 50004, N'No hay metodos de pago activos.', 1;

-- Productos: todos (historial) y solo los vendibles ahora (cuentas abiertas)
CREATE TABLE #ProdAll (rn INT PRIMARY KEY, IdProducto INT, Precio DECIMAL(10,2));
INSERT #ProdAll SELECT ROW_NUMBER() OVER (ORDER BY IdProducto), IdProducto, Precio FROM dbo.Producto;
DECLARE @nAll INT = (SELECT COUNT(*) FROM #ProdAll);
CREATE TABLE #ProdDisp (rn INT PRIMARY KEY, IdProducto INT, Precio DECIMAL(10,2));
INSERT #ProdDisp SELECT ROW_NUMBER() OVER (ORDER BY p.IdProducto), p.IdProducto, p.Precio
FROM dbo.Producto p JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria WHERE p.Disponible = 1 AND c.Activa = 1;
DECLARE @nDisp INT = (SELECT COUNT(*) FROM #ProdDisp);
IF @nAll < 8 OR @nDisp < 8 THROW 50005, N'Hacen falta al menos 8 productos (y 8 disponibles) en el catalogo.', 1;
-- Paso para recorrer el catalogo sin repetir productos dentro de un pedido (coprimo con el tamano)
DECLARE @stAll INT = CASE WHEN @nAll % 7 <> 0 THEN 7 WHEN @nAll % 11 <> 0 THEN 11 ELSE 13 END;
DECLARE @stDisp INT = CASE WHEN @nDisp % 7 <> 0 THEN 7 WHEN @nDisp % 11 <> 0 THEN 11 ELSE 13 END;

-- Mesas habilitadas y pares adyacentes (misma regla que la API: distancia PosX/PosY <= 200)
CREATE TABLE #Mesas (IdMesa INT PRIMARY KEY, Num INT, Cap INT, X INT, Y INT);
INSERT #Mesas SELECT IdMesa, NumeroMesa, Capacidad, PosX, PosY FROM dbo.Mesa WHERE Estado <> N'Deshabilitada';
IF NOT EXISTS (SELECT 1 FROM #Mesas) THROW 50006, N'No hay mesas habilitadas.', 1;
CREATE TABLE #Pares (A INT, B INT, Cap INT);
INSERT #Pares
SELECT a.IdMesa, b.IdMesa, a.Cap + b.Cap
FROM #Mesas a JOIN #Mesas b ON a.IdMesa < b.IdMesa
WHERE SQRT(POWER(CAST(a.X - b.X AS FLOAT), 2) + POWER(CAST(a.Y - b.Y AS FLOAT), 2)) <= 200;

-- Ocupacion de mesas (para no sobreponer reservas): lo que ya existe en la base
CREATE TABLE #Ocup (IdMesa INT, Ini DATETIME2(0), Fin DATETIME2(0));
INSERT #Ocup
SELECT rm.IdMesa,
       s.Ini,
       DATEADD(MINUTE, @Duracion, s.Ini)
FROM dbo.Reserva r
JOIN dbo.ReservaMesa rm ON rm.IdReserva = r.IdReserva
JOIN dbo.EstadoReserva e ON e.IdEstadoReserva = r.IdEstadoReserva
CROSS APPLY (SELECT CASE WHEN r.FechaAtencion IS NOT NULL AND r.FechaAtencion < DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), r.HoraReserva), CAST(r.FechaReserva AS DATETIME2(0)))
                         THEN CAST(r.FechaAtencion AS DATETIME2(0))
                         ELSE DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), r.HoraReserva), CAST(r.FechaReserva AS DATETIME2(0))) END AS Ini) s
WHERE e.Nombre IN (N'Confirmada', N'Finalizada', N'No Show', N'Pendiente');

-- Dias en que el restaurante abre (HorarioOperacion activo, sin cierre fijo ni eventual)
CREATE TABLE #Dias (Fecha DATE PRIMARY KEY, Apertura TIME(0), Cierre TIME(0));
;WITH n AS (
    SELECT TOP (DATEDIFF(DAY, @Desde, @Hasta) + 1) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT #Dias (Fecha, Apertura, Cierre)
SELECT x.Fecha, h.HoraApertura, h.HoraCierre
FROM (SELECT DATEADD(DAY, i, @Desde) AS Fecha FROM n) x
JOIN dbo.HorarioOperacion h ON h.Activo = 1 AND h.DiaSemana = DATEDIFF(DAY, '19000107', x.Fecha) % 7
WHERE NOT EXISTS (SELECT 1 FROM dbo.CierreFijo c WHERE c.Activo = 1 AND c.DiaSemana = DATEDIFF(DAY, '19000107', x.Fecha) % 7)
  AND NOT EXISTS (SELECT 1 FROM dbo.CierreEventual e WHERE e.Activo = 1 AND e.Fecha = x.Fecha);

DECLARE @nDias INT, @listaDias NVARCHAR(MAX);
SELECT @nDias = COUNT(*) FROM #Dias;
SELECT @listaDias = STRING_AGG(CONVERT(VARCHAR(5), Fecha, 5), ' ') WITHIN GROUP (ORDER BY Fecha) FROM #Dias;
PRINT 'Dias abiertos en el periodo ' + CONVERT(VARCHAR(10), @Desde, 23) + ' a ' + CONVERT(VARCHAR(10), @Hasta, 23) + ': ' + CAST(@nDias AS VARCHAR(10)) + ' (' + ISNULL(@listaDias, 'ninguno') + ')';
IF NOT EXISTS (SELECT 1 FROM #Dias) PRINT 'ADVERTENCIA: ningun dia abierto en el periodo; solo se sembrara el escenario de hoy.';

-- Horas de llegada validas: igual que el calendario de disponibilidad
CREATE TABLE #Inicios (Fecha DATE, Hora TIME(0), PRIMARY KEY (Fecha, Hora));
;WITH k AS (SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects)
INSERT #Inicios (Fecha, Hora)
SELECT d.Fecha, DATEADD(MINUTE, k.i * @Intervalo, d.Apertura)
FROM #Dias d CROSS JOIN k
WHERE DATEDIFF(MINUTE, d.Apertura, d.Cierre) >= k.i * @Intervalo + @Duracion;

/* ============================================================
   3. PLAN de reservas (#Res) y de clientes
   ============================================================ */
CREATE TABLE #Res (
    n INT IDENTITY(1,1) PRIMARY KEY,
    Fecha DATE NOT NULL, Hora TIME(0) NOT NULL, Ini DATETIME2(0) NOT NULL,
    Llegada DATETIME2(0) NULL,
    Personas INT NOT NULL, Estado NVARCHAR(20) NOT NULL,
    EsWalkIn BIT NOT NULL, Motivo NVARCHAR(100) NULL, IdCierreEventual INT NULL,
    Mesa1 INT NULL, Mesa2 INT NULL,
    ClienteIdx INT NULL, Obs NVARCHAR(300) NOT NULL, Creada DATETIME2(0) NOT NULL,
    VentaTipo NVARCHAR(12) NOT NULL,      -- NONE / COBRADA / ANULADA / SINCONSUMO / ABIERTA
    NPedidos INT NOT NULL DEFAULT 0, Semilla INT NOT NULL DEFAULT 0, StayMin INT NULL);

-- 36 clientes de prueba (se crean los que falten)
CREATE TABLE #CliPlan (idx INT PRIMARY KEY, Nombre NVARCHAR(80), Apellido NVARCHAR(80));
INSERT #CliPlan (idx, Nombre, Apellido) VALUES
 (1, N'Valeria', N'Mora Vargas'), (2, N'Diego', N'Solano Rojas'), (3, N'Camila', N'Jiménez Castro'), (4, N'Andrés', N'Araya Quesada'),
 (5, N'Sofía', N'Chaves Salazar'), (6, N'Mauricio', N'Rodríguez Núñez'), (7, N'Daniela', N'Hernández Brenes'), (8, N'Esteban', N'Alfaro Campos'),
 (9, N'Paola', N'Calderón Mena'), (10, N'Gabriel', N'Ugalde Zúñiga'), (11, N'Fernanda', N'Vega Barrantes'), (12, N'Rodrigo', N'Cordero Pacheco'),
 (13, N'Carolina', N'Sánchez Arias'), (14, N'Luis', N'Fallas Madrigal'), (15, N'Mariana', N'Cruz Villalobos'), (16, N'Jorge', N'Ramírez Bolaños'),
 (17, N'Adriana', N'Murillo Segura'), (18, N'Kevin', N'Salas Porras'), (19, N'Natalia', N'Carvajal Soto'), (20, N'Allan', N'Quirós Montero'),
 (21, N'Karla', N'Badilla Esquivel'), (22, N'José', N'Navarro Picado'), (23, N'Melissa', N'Elizondo Ureña'), (24, N'Óscar', N'Zamora Leitón'),
 (25, N'Tatiana', N'Gamboa Retana'), (26, N'Randall', N'Monge Obando'), (27, N'Lucía', N'Acuña Brenes'), (28, N'Marco', N'Delgado Rivera'),
 (29, N'Priscila', N'Orozco Matamoros'), (30, N'Sebastián', N'Venegas Cascante'), (31, N'Yendry', N'Artavia Corrales'), (32, N'Bryan', N'Segura Mata'),
 (33, N'Stephanie', N'Pizarro Lobo'), (34, N'Alonso', N'Rojas Fonseca'), (35, N'Vanessa', N'Bonilla Aguilar'), (36, N'Gerardo', N'Camacho Loría');
DECLARE @nCli INT = 36;

/* ---- 3.1 Dias pasados y futuros: reservas por horario valido ---- */
DECLARE @f DATE, @h TIME(0), @k INT, @intentos INT, @h1 INT, @b INT, @pers INT;
DECLARE @ini DATETIME2(0), @fin DATETIME2(0), @m1 INT, @m2 INT;
DECLARE @estado NVARCHAR(20), @motivo NVARCHAR(100), @venta NVARCHAR(12), @walk BIT, @llegada DATETIME2(0), @stay INT, @obs NVARCHAR(300), @npedidos INT, @creada DATETIME2(0);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT Fecha, Hora FROM #Inicios WHERE Fecha <> @Hoy ORDER BY Fecha, Hora;
OPEN cur;
FETCH NEXT FROM cur INTO @f, @h;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @intentos = CASE WHEN ABS(CHECKSUM(@f, @h, 11)) % 100 < 28 THEN 0 WHEN ABS(CHECKSUM(@f, @h, 11)) % 100 < 83 THEN 1 ELSE 2 END;   -- 0 a 2 reservas por horario
    SET @k = 1;
    WHILE @k <= @intentos
    BEGIN
        SET @h1 = ABS(CHECKSUM(@f, @h, @k, 101));
        SET @pers = CHOOSE(1 + @h1 % 12, 2, 2, 2, 3, 4, 4, 5, 6, 2, 8, 4, 1);
        SET @ini = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), @h), CAST(@f AS DATETIME2(0)));
        SET @fin = DATEADD(MINUTE, @Duracion, @ini);
        SET @b = ABS(CHECKSUM(@f, @h, @k, 202)) % 100;
        SET @walk = CASE WHEN @f < @Hoy AND ABS(CHECKSUM(@f, @h, @k, 303)) % 100 < 14 THEN 1 ELSE 0 END;
        SET @motivo = NULL; SET @venta = N'NONE'; SET @npedidos = 0; SET @llegada = NULL; SET @stay = NULL;

        IF @f < @Hoy
        BEGIN
            -- Dias pasados: todo esta resuelto (finalizadas, No Show y canceladas)
            SET @estado = CASE WHEN @walk = 1 OR @b < 74 OR @b >= 96 THEN N'Finalizada'
                               WHEN @b < 82 THEN N'No Show'
                               ELSE N'Cancelada' END;
            SET @motivo = CASE WHEN @estado = N'No Show' THEN N'Marcada como No Show por el administrador'
                               WHEN @estado = N'Cancelada' THEN CASE WHEN @b < 91 THEN N'Cancelada por el cliente' ELSE N'Cancelada por el administrador' END
                               ELSE NULL END;
            IF @estado = N'Finalizada'
            BEGIN
                -- 6 % anuladas, 6 % sin consumo, el resto cobradas con consumo
                SET @venta = CASE WHEN @walk = 0 AND @b >= 62 AND @b < 68 THEN N'ANULADA'
                                  WHEN @walk = 0 AND @b >= 68 AND @b < 74 THEN N'SINCONSUMO'
                                  WHEN @walk = 1 AND ABS(CHECKSUM(@f, @h, @k, 404)) % 100 < 7 THEN N'ANULADA'
                                  ELSE N'COBRADA' END;
                IF @venta = N'SINCONSUMO' SET @motivo = N'Liberada sin consumo';
                SET @llegada = DATEADD(MINUTE, CASE WHEN @walk = 1 THEN 0 ELSE ABS(CHECKSUM(@f, @h, @k, 505)) % 6 - 1 END, @ini);
                SET @stay = 50 + ABS(CHECKSUM(@f, @h, @k, 606)) % 33;
                SET @npedidos = CASE WHEN @venta = N'SINCONSUMO' THEN 0 ELSE 1 + ABS(CHECKSUM(@f, @h, @k, 707)) % 2 END;
            END
        END
        ELSE
        BEGIN
            -- Dias futuros: confirmadas y algunas canceladas
            SET @estado = CASE WHEN @b < 88 THEN N'Confirmada' ELSE N'Cancelada' END;
            SET @motivo = CASE WHEN @estado = N'Cancelada' THEN CASE WHEN @b < 97 THEN N'Cancelada por el cliente' ELSE N'Cancelada por el administrador' END END;
        END

        -- Mesas: la mas ajustada libre; si no, un par adyacente libre. Las canceladas no ocupan mesa.
        SET @m1 = NULL; SET @m2 = NULL;
        IF @estado <> N'Cancelada'
        BEGIN
            SELECT TOP (1) @m1 = m.IdMesa FROM #Mesas m
            WHERE m.Cap >= @pers
              AND NOT EXISTS (SELECT 1 FROM #Ocup o WHERE o.IdMesa = m.IdMesa AND o.Ini < @fin AND o.Fin > @ini)
            ORDER BY m.Cap, ABS(CHECKSUM(m.IdMesa, @h1)) % 7;
            IF @m1 IS NULL
                SELECT TOP (1) @m1 = p.A, @m2 = p.B FROM #Pares p
                WHERE p.Cap >= @pers
                  AND NOT EXISTS (SELECT 1 FROM #Ocup o WHERE o.IdMesa IN (p.A, p.B) AND o.Ini < @fin AND o.Fin > @ini)
                ORDER BY p.Cap;
        END

        IF @estado = N'Cancelada' OR @m1 IS NOT NULL
        BEGIN
            SET @obs = N'[SEED]' + CASE ABS(CHECKSUM(@f, @h, @k, 808)) % 16
                WHEN 0 THEN N' Cumpleaños' WHEN 1 THEN N' Silla para bebé' WHEN 2 THEN N' Alergia a mariscos'
                WHEN 3 THEN N' Aniversario' WHEN 4 THEN N' Prefiere mesa tranquila' ELSE N'' END;
            SET @creada = DATEADD(MINUTE, -(60 + ABS(CHECKSUM(@f, @h, @k, 909)) % 14400), @ini);   -- hasta 10 dias antes
            IF @creada > @Ahora SET @creada = DATEADD(HOUR, -1, @Ahora);

            INSERT #Res (Fecha, Hora, Ini, Llegada, Personas, Estado, EsWalkIn, Motivo, Mesa1, Mesa2, ClienteIdx, Obs, Creada, VentaTipo, NPedidos, Semilla, StayMin)
            VALUES (@f, @h, @ini, @llegada, @pers, @estado, @walk, @motivo, @m1, @m2,
                    CASE WHEN @walk = 1 THEN NULL ELSE 1 + ABS(CHECKSUM(@f, @h, @k, 1001)) % @nCli END,
                    CASE WHEN @walk = 1 THEN N'[SEED] Walk-in' ELSE @obs END,
                    CASE WHEN @walk = 1 THEN @ini ELSE @creada END, @venta, @npedidos, ABS(CHECKSUM(@f, @h, @k, 1101)), @stay);

            IF @m1 IS NOT NULL INSERT #Ocup VALUES (@m1, @ini, @fin);
            IF @m2 IS NOT NULL INSERT #Ocup VALUES (@m2, @ini, @fin);
        END

        SET @k += 1;
    END
    FETCH NEXT FROM cur INTO @f, @h;
END
CLOSE cur; DEALLOCATE cur;

/* ---- 3.2 Reservas canceladas por cierre administrativo (cada CierreEventual del periodo) ---- */
INSERT #Res (Fecha, Hora, Ini, Personas, Estado, EsWalkIn, Motivo, IdCierreEventual, ClienteIdx, Obs, Creada, VentaTipo, Semilla)
SELECT e.Fecha, hr.Hora, hr.Ini, 2 + (s.i * 2) % 5, N'Cancelada', 0, N'Cierre administrativo', e.IdCierreEventual,
       1 + ABS(CHECKSUM(e.IdCierreEventual, s.i, 1201)) % @nCli, N'[SEED] Cancelada por cierre administrativo',
       DATEADD(DAY, -(3 + s.i), hr.Ini), N'NONE', ABS(CHECKSUM(e.IdCierreEventual, s.i, 1301))
FROM dbo.CierreEventual e
JOIN dbo.HorarioOperacion h ON h.Activo = 1 AND h.DiaSemana = DATEDIFF(DAY, '19000107', e.Fecha) % 7
CROSS JOIN (VALUES (0), (1), (2)) s(i)
CROSS APPLY (SELECT DATEADD(MINUTE, s.i * 60, h.HoraApertura) AS Hora) t
CROSS APPLY (SELECT t.Hora AS Hora, DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), t.Hora), CAST(e.Fecha AS DATETIME2(0))) AS Ini) hr
WHERE e.Activo = 1 AND e.Fecha BETWEEN @Desde AND @Hasta
  AND DATEDIFF(MINUTE, h.HoraApertura, h.HoraCierre) >= s.i * 60 + @Duracion;

/* ---- 3.3 HOY: escenario de revision relativo a la hora actual (ignora horario y cierres) ---- */
IF @IncluirHoy = 1
BEGIN
    CREATE TABLE #Esc (
        Clave NVARCHAR(20), OffIni INT, Mesa1Num INT, Mesa2Num INT NULL, Personas INT, Estado NVARCHAR(20), Walk BIT,
        OffLlegada INT NULL, VentaTipo NVARCHAR(12), NPedidos INT, Stay INT NULL, Motivo NVARCHAR(100) NULL, Nota NVARCHAR(120));
    -- OffIni / OffLlegada: minutos respecto a la hora actual
    INSERT #Esc VALUES
     (N'union',      -480, 1,    2,    4, N'Finalizada', 0, -478, N'COBRADA',    3, 75,   NULL, N'Grupo en mesas unidas 1+2, ya cobrado'),
     (N'fin1',       -360, 1,    NULL, 2, N'Finalizada', 0, -358, N'COBRADA',    2, 70,   NULL, N'Cobrada hoy'),
     (N'fin2',       -330, 2,    NULL, 2, N'Finalizada', 1, -330, N'COBRADA',    1, 60,   NULL, N'Walk-in cobrado hoy'),
     (N'fin4',       -270, 5,    NULL, 8, N'Finalizada', 0, -268, N'COBRADA',    3, 80,   NULL, N'Grupo grande cobrado hoy'),
     (N'fin3',       -300, 4,    NULL, 4, N'Finalizada', 0, -298, N'ANULADA',    2, 65,   NULL, N'Venta anulada por error de cobro'),
     (N'sinconsumo', -240, 1,    NULL, 2, N'Finalizada', 0, -238, N'SINCONSUMO', 0, 25,   N'Liberada sin consumo', N'Se retiraron sin pedir'),
     (N'noshow',     -200, 4,    NULL, 4, N'No Show',    0, NULL, N'NONE',       0, NULL, N'Marcada como No Show por el administrador', N'No se presentaron'),
     (N'cancelada',  -180, NULL, NULL, 3, N'Cancelada',  0, NULL, N'NONE',       0, NULL, N'Cancelada por el cliente', N'Canceló hoy'),
     (N'cuenta',      -50, 1,    NULL, 2, N'Confirmada', 0, -48,  N'ABIERTA',    2, NULL, NULL, N'Ocupada con cuenta abierta'),
     (N'sinpedido',   -15, 2,    NULL, 2, N'Confirmada', 1, -15,  N'NONE',       0, NULL, NULL, N'Ocupada sin pedido (walk-in)'),
     (N'espera',      -10, 4,    NULL, 4, N'Confirmada', 0, NULL, N'NONE',       0, NULL, NULL, N'Esperando la llegada del cliente'),
     (N'excede',     -120, 5,    NULL, 6, N'Confirmada', 0, -115, N'ABIERTA',    3, NULL, NULL, N'Excede el turno con cuenta abierta'),
     (N'proxima',      25, 3,    NULL, 3, N'Confirmada', 0, NULL, N'NONE',       0, NULL, NULL, N'Reserva proxima');

    DECLARE @eClave NVARCHAR(20), @eOff INT, @eM1 INT, @eM2 INT, @ePers INT, @eEst NVARCHAR(20), @eWalk BIT, @eLleg INT, @eVenta NVARCHAR(12), @eNPed INT, @eStay INT, @eMot NVARCHAR(100), @eNota NVARCHAR(120);
    DECLARE @eIni DATETIME2(0), @eFin DATETIME2(0), @idM1 INT, @idM2 INT, @eSem INT;
    DECLARE curE CURSOR LOCAL FAST_FORWARD FOR SELECT Clave, OffIni, Mesa1Num, Mesa2Num, Personas, Estado, Walk, OffLlegada, VentaTipo, NPedidos, Stay, Motivo, Nota FROM #Esc ORDER BY OffIni;
    OPEN curE;
    FETCH NEXT FROM curE INTO @eClave, @eOff, @eM1, @eM2, @ePers, @eEst, @eWalk, @eLleg, @eVenta, @eNPed, @eStay, @eMot, @eNota;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @eIni = DATEADD(MINUTE, @eOff, @Ahora);
        SET @eFin = DATEADD(MINUTE, @Duracion, @eIni);
        SET @idM1 = NULL; SET @idM2 = NULL;
        SELECT @idM1 = IdMesa FROM #Mesas WHERE Num = @eM1;
        SELECT @idM2 = IdMesa FROM #Mesas WHERE Num = @eM2;
        SET @eSem = ABS(CHECKSUM(@eClave, 1401));

        IF @eEst = N'Cancelada'
        BEGIN
            INSERT #Res (Fecha, Hora, Ini, Personas, Estado, EsWalkIn, Motivo, ClienteIdx, Obs, Creada, VentaTipo, Semilla)
            VALUES (CAST(@eIni AS DATE), CAST(@eIni AS TIME(0)), @eIni, @ePers, @eEst, 0, @eMot, 1 + @eSem % @nCli, N'[SEED] HOY: ' + @eNota, DATEADD(DAY, -3, @eIni), N'NONE', @eSem);
        END
        ELSE IF @idM1 IS NULL OR (@eM2 IS NOT NULL AND @idM2 IS NULL)
        BEGIN
            PRINT 'Escenario de hoy "' + @eClave + '" omitido: la mesa ' + CAST(@eM1 AS VARCHAR(5)) + ' no existe o esta deshabilitada.';
        END
        ELSE IF EXISTS (SELECT 1 FROM #Ocup o WHERE o.IdMesa IN (@idM1, ISNULL(@idM2, @idM1)) AND o.Ini < @eFin AND o.Fin > @eIni)
        BEGIN
            PRINT 'Escenario de hoy "' + @eClave + '" omitido: la mesa ' + CAST(@eM1 AS VARCHAR(5)) + ' ya esta ocupada en ese horario por otra reserva.';
        END
        ELSE
        BEGIN
            INSERT #Res (Fecha, Hora, Ini, Llegada, Personas, Estado, EsWalkIn, Motivo, Mesa1, Mesa2, ClienteIdx, Obs, Creada, VentaTipo, NPedidos, Semilla, StayMin)
            VALUES (CAST(@eIni AS DATE), CAST(@eIni AS TIME(0)), @eIni, DATEADD(MINUTE, @eLleg, @Ahora), @ePers, @eEst, @eWalk, @eMot, @idM1, @idM2,
                    CASE WHEN @eWalk = 1 THEN NULL ELSE 1 + @eSem % @nCli END, N'[SEED] HOY: ' + @eNota,
                    CASE WHEN @eWalk = 1 THEN @eIni ELSE DATEADD(DAY, -2, @eIni) END, @eVenta, @eNPed, @eSem, @eStay);
            INSERT #Ocup VALUES (@idM1, @eIni, @eFin);
            IF @idM2 IS NOT NULL INSERT #Ocup VALUES (@idM2, @eIni, @eFin);
        END
        FETCH NEXT FROM curE INTO @eClave, @eOff, @eM1, @eM2, @ePers, @eEst, @eWalk, @eLleg, @eVenta, @eNPed, @eStay, @eMot, @eNota;
    END
    CLOSE curE; DEALLOCATE curE;
END

/* ============================================================
   4. INSERCION: clientes, reservas y mesas
   ============================================================ */
INSERT INTO dbo.Cliente (Nombre, Apellido, Telefono, CorreoElectronico, FechaCreacion)
SELECT p.Nombre, p.Apellido,
       N'8' + RIGHT(N'0000000' + CAST(1000000 + (p.idx * 7919) % 8999999 AS NVARCHAR(10)), 7),
       N'cliente' + CAST(p.idx AS NVARCHAR(5)) + N'@seed.montecarlo.test',
       DATEADD(DAY, -(20 + p.idx), @Ahora)
FROM #CliPlan p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Cliente c WHERE c.CorreoElectronico = N'cliente' + CAST(p.idx AS NVARCHAR(5)) + N'@seed.montecarlo.test');

CREATE TABLE #Cli (idx INT PRIMARY KEY, IdCliente INT);
INSERT #Cli SELECT p.idx, c.IdCliente FROM #CliPlan p JOIN dbo.Cliente c ON c.CorreoElectronico = N'cliente' + CAST(p.idx AS NVARCHAR(5)) + N'@seed.montecarlo.test';

-- Codigo de reserva: MC-S + 4 caracteres del alfabeto de la aplicacion (sin vocales)
DECLARE @Alfa NVARCHAR(40) = N'BCDFGHJKLMNPQRSTVWXYZ23456789';
CREATE TABLE #MapR (n INT PRIMARY KEY, IdReserva INT);

MERGE dbo.Reserva AS t
USING (
    SELECT r.n, r.Fecha, r.Hora, r.Personas, r.EsWalkIn, r.Llegada, r.Motivo, r.IdCierreEventual, r.Obs, r.Creada,
           CASE WHEN r.EsWalkIn = 1 THEN NULL
                ELSE N'MC-S' + SUBSTRING(@Alfa, 1 + (r.n / 24389) % 29, 1) + SUBSTRING(@Alfa, 1 + (r.n / 841) % 29, 1)
                             + SUBSTRING(@Alfa, 1 + (r.n / 29) % 29, 1) + SUBSTRING(@Alfa, 1 + r.n % 29, 1) END AS Codigo,
           CASE WHEN r.EsWalkIn = 1 THEN @Walkin ELSE c.IdCliente END AS IdCliente,
           CASE r.Estado WHEN N'Confirmada' THEN @EstConf WHEN N'Cancelada' THEN @EstCanc WHEN N'Finalizada' THEN @EstFin ELSE @EstNoSh END AS IdEstado
    FROM #Res r LEFT JOIN #Cli c ON c.idx = r.ClienteIdx
) AS s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (CodigoReserva, IdCliente, IdEstadoReserva, FechaReserva, HoraReserva, CantidadPersonas, RequiereAsignacionManual, EsWalkIn, FechaAtencion, MotivoCancelacion, IdCierreEventual, Observaciones, FechaCreacion)
    VALUES (s.Codigo, s.IdCliente, s.IdEstado, s.Fecha, s.Hora, s.Personas, 0, s.EsWalkIn, s.Llegada, s.Motivo, s.IdCierreEventual, s.Obs, s.Creada)
    OUTPUT s.n, inserted.IdReserva INTO #MapR (n, IdReserva);

INSERT INTO dbo.ReservaMesa (IdReserva, IdMesa)
SELECT m.IdReserva, x.IdMesa FROM #Res r JOIN #MapR m ON m.n = r.n CROSS APPLY (VALUES (r.Mesa1), (r.Mesa2)) x(IdMesa) WHERE x.IdMesa IS NOT NULL;

/* ============================================================
   5. VENTAS: plan (#VentaPlan), pedidos y lineas
   ============================================================ */
CREATE TABLE #VentaPlan (
    vn INT IDENTITY(1,1) PRIMARY KEY, n INT NULL, Tipo NVARCHAR(10), Estado NVARCHAR(10),
    Apertura DATETIME2(0), Cobro DATETIME2(0) NULL, IdMetodoPago INT NULL, IdUsuReg INT, IdUsuCobro INT NULL,
    FechaAnul DATETIME2(0) NULL, MotivoAnul NVARCHAR(300) NULL, IdUsuAnul INT NULL,
    NPedidos INT, Gap INT, UsaDisp BIT, Semilla INT);

-- 5.1 Ventas de reservas (consumo)
INSERT #VentaPlan (n, Tipo, Estado, Apertura, Cobro, IdMetodoPago, IdUsuReg, IdUsuCobro, FechaAnul, MotivoAnul, IdUsuAnul, NPedidos, Gap, UsaDisp, Semilla)
SELECT r.n, N'Consumo',
       CASE r.VentaTipo WHEN N'ABIERTA' THEN N'Abierta' WHEN N'ANULADA' THEN N'Anulada' ELSE N'Cobrada' END,
       CASE WHEN r.VentaTipo = N'SINCONSUMO' THEN r.Llegada ELSE DATEADD(MINUTE, 5, r.Llegada) END,
       CASE WHEN r.VentaTipo = N'ABIERTA' THEN NULL ELSE DATEADD(MINUTE, r.StayMin, r.Llegada) END,
       CASE WHEN r.VentaTipo IN (N'COBRADA', N'ANULADA') THEN mt.IdMetodoPago END,
       u.IdUsuario,
       CASE WHEN r.VentaTipo = N'ABIERTA' THEN NULL ELSE u.IdUsuario END,
       CASE WHEN r.VentaTipo = N'ANULADA' THEN DATEADD(MINUTE, 10 + r.Semilla % 40, DATEADD(MINUTE, r.StayMin, r.Llegada)) END,
       CASE WHEN r.VentaTipo = N'ANULADA' THEN CHOOSE(1 + r.Semilla % 4, N'Mesa equivocada', N'Cobro duplicado', N'Error en el método de pago', N'Producto cobrado por error') END,
       CASE WHEN r.VentaTipo = N'ANULADA' THEN g.IdUsuario END,
       r.NPedidos,
       CASE WHEN r.VentaTipo = N'ABIERTA' THEN (DATEDIFF(MINUTE, r.Llegada, @Ahora) - 8) / NULLIF(r.NPedidos, 0)
            WHEN r.NPedidos = 0 THEN 0
            ELSE ((r.StayMin - 5) / r.NPedidos) - 3 END,
       CASE WHEN r.VentaTipo = N'ABIERTA' THEN 1 ELSE 0 END,
       r.Semilla
FROM #Res r
JOIN #Usu u ON u.rn = 1 + (r.Semilla / 7) % @nUsu
JOIN #UsuG g ON g.rn = 1 + (r.Semilla / 11) % @nUsuG
JOIN #Met mt ON mt.rn = 1 + (CASE WHEN r.Semilla % 10 < 3 THEN 0 WHEN r.Semilla % 10 < 8 THEN 1 ELSE 2 END) % @nMet
WHERE r.VentaTipo <> N'NONE';

-- 5.2 Ventas para llevar: 1 a 2 por dia abierto pasado, dentro del horario, mas las de hoy
;WITH k AS (SELECT TOP (2) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT #VentaPlan (n, Tipo, Estado, Apertura, Cobro, IdMetodoPago, IdUsuReg, IdUsuCobro, FechaAnul, MotivoAnul, IdUsuAnul, NPedidos, Gap, UsaDisp, Semilla)
SELECT NULL, N'Takeout', CASE WHEN s.Semilla % 100 < 8 THEN N'Anulada' ELSE N'Cobrada' END,
       DATEADD(MINUTE, -5, s.Cobro), s.Cobro, mt.IdMetodoPago, u.IdUsuario, u.IdUsuario,
       CASE WHEN s.Semilla % 100 < 8 THEN DATEADD(MINUTE, 15, s.Cobro) END,
       CASE WHEN s.Semilla % 100 < 8 THEN N'Pedido cancelado por el cliente' END,
       CASE WHEN s.Semilla % 100 < 8 THEN g.IdUsuario END,
       1, 0, 0, s.Semilla
FROM (
    SELECT d.Fecha, k.i,
           ABS(CHECKSUM(d.Fecha, k.i, 1501)) AS Semilla,
           DATEADD(MINUTE, 10 + ABS(CHECKSUM(d.Fecha, k.i, 1601)) % (DATEDIFF(MINUTE, d.Apertura, d.Cierre) - 25),
                   DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), d.Apertura), CAST(d.Fecha AS DATETIME2(0)))) AS Cobro
    FROM #Dias d CROSS JOIN k
    WHERE d.Fecha < @Hoy AND k.i <= 1 + ABS(CHECKSUM(d.Fecha, 1701)) % 2
) s
JOIN #Usu u ON u.rn = 1 + (s.Semilla / 7) % @nUsu
JOIN #UsuG g ON g.rn = 1 + (s.Semilla / 11) % @nUsuG
JOIN #Met mt ON mt.rn = 1 + (CASE WHEN s.Semilla % 10 < 3 THEN 0 WHEN s.Semilla % 10 < 8 THEN 1 ELSE 2 END) % @nMet;

IF @IncluirHoy = 1
BEGIN
    INSERT #VentaPlan (n, Tipo, Estado, Apertura, Cobro, IdMetodoPago, IdUsuReg, IdUsuCobro, FechaAnul, MotivoAnul, IdUsuAnul, NPedidos, Gap, UsaDisp, Semilla)
    SELECT NULL, N'Takeout', t.Estado, DATEADD(MINUTE, t.Mins - 5, @Ahora), DATEADD(MINUTE, t.Mins, @Ahora), mt.IdMetodoPago, u.IdUsuario, u.IdUsuario,
           CASE WHEN t.Estado = N'Anulada' THEN DATEADD(MINUTE, t.Mins + 12, @Ahora) END,
           CASE WHEN t.Estado = N'Anulada' THEN N'Cliente no recogió el pedido' END,
           CASE WHEN t.Estado = N'Anulada' THEN g.IdUsuario END, 1, 0, 0, ABS(CHECKSUM(t.Mins, 1801))
    FROM (VALUES (-200, N'Cobrada', 1), (-150, N'Cobrada', 3), (-95, N'Anulada', 2)) t(Mins, Estado, MetIdx)
    JOIN #Usu u ON u.rn = 1 + (ABS(t.Mins) % @nUsu) + 0
    JOIN #UsuG g ON g.rn = 1 + (ABS(t.Mins) % @nUsuG)
    JOIN #Met mt ON mt.rn = 1 + (t.MetIdx - 1) % @nMet;
END

-- 5.3 Insertar ventas
CREATE TABLE #MapV (vn INT PRIMARY KEY, IdVenta INT);
MERGE dbo.Venta AS t
USING (
    SELECT vp.vn, vp.Tipo, vp.Estado, mr.IdReserva, vp.IdMetodoPago, vp.IdUsuReg, vp.Apertura, vp.Cobro, vp.IdUsuCobro, vp.MotivoAnul, vp.FechaAnul, vp.IdUsuAnul
    FROM #VentaPlan vp LEFT JOIN #MapR mr ON mr.n = vp.n
) AS s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (TipoVenta, Estado, IdReserva, IdMetodoPago, Total, IdUsuarioRegistro, FechaVenta, FechaCobro, IdUsuarioCobro, MotivoAnulacion, FechaAnulacion, IdUsuarioAnulacion)
    VALUES (s.Tipo, s.Estado, s.IdReserva, s.IdMetodoPago, 0, s.IdUsuReg, s.Apertura, s.Cobro, s.IdUsuCobro, s.MotivoAnul, s.FechaAnul, s.IdUsuAnul)
    OUTPUT s.vn, inserted.IdVenta INTO #MapV (vn, IdVenta);

-- 5.4 Pedidos (1 a 2 por venta; los escenarios de hoy llegan a 3) con su hora y quien los registro
CREATE TABLE #PedPlan (pk INT IDENTITY(1,1) PRIMARY KEY, vn INT, Num INT, Fecha DATETIME2(0), IdUsuario INT, Semilla INT, UsaDisp BIT, Estado NVARCHAR(10));
INSERT #PedPlan (vn, Num, Fecha, IdUsuario, Semilla, UsaDisp, Estado)
SELECT vp.vn, k.i,
       CASE WHEN vp.Tipo = N'Takeout' THEN DATEADD(MINUTE, 1, vp.Apertura)
            ELSE DATEADD(MINUTE, (k.i - 1) * vp.Gap, vp.Apertura) END,
       CASE WHEN k.i = 1 THEN vp.IdUsuReg ELSE u.IdUsuario END,
       ABS(CHECKSUM(vp.vn, k.i, 1901)), vp.UsaDisp, vp.Estado
FROM #VentaPlan vp
CROSS JOIN (VALUES (1), (2), (3)) k(i)
JOIN #Usu u ON u.rn = 1 + ABS(CHECKSUM(vp.vn, k.i, 2001)) % @nUsu
WHERE k.i <= vp.NPedidos;

CREATE TABLE #MapP (pk INT PRIMARY KEY, IdPedido INT);
MERGE dbo.PedidoVenta AS t
USING (SELECT pp.pk, mv.IdVenta, pp.IdUsuario, pp.Fecha FROM #PedPlan pp JOIN #MapV mv ON mv.vn = pp.vn) AS s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (IdVenta, IdUsuario, Fecha, ClaveIdempotencia, Nota)
    VALUES (s.IdVenta, s.IdUsuario, s.Fecha, NEWID(), N'[SEED]')
    OUTPUT s.pk, inserted.IdPedido INTO #MapP (pk, IdPedido);

-- 5.5 Lineas: 1 a 3 productos distintos por pedido; la primera nunca se anula
INSERT INTO dbo.DetalleVenta (IdVenta, IdPedido, IdProducto, Cantidad, PrecioUnitario, Anulado, MotivoAnulacion, FechaAnulacion, IdUsuarioAnulacion)
SELECT mv.IdVenta, mp.IdPedido, p.IdProducto,
       1 + ABS(CHECKSUM(pp.pk, k.i, 2101)) % 3,
       CASE WHEN CAST(pp.Fecha AS DATE) < @Cambio THEN CAST(CASE WHEN ROUND(p.Precio * 0.95 / 100, 0) * 100 < 100 THEN 100 ELSE ROUND(p.Precio * 0.95 / 100, 0) * 100 END AS DECIMAL(10,2)) ELSE p.Precio END,
       x.Anula,
       CASE WHEN x.Anula = 1 THEN CHOOSE(1 + ABS(CHECKSUM(pp.pk, k.i, 2201)) % 2, N'Pedido por error', N'El cliente cambió de opinión') END,
       CASE WHEN x.Anula = 1 THEN DATEADD(MINUTE, 6, pp.Fecha) END,
       CASE WHEN x.Anula = 1 THEN pp.IdUsuario END
FROM #PedPlan pp
JOIN #MapP mp ON mp.pk = pp.pk
JOIN #MapV mv ON mv.vn = pp.vn
CROSS JOIN (VALUES (1), (2), (3), (4)) k(i)
CROSS APPLY (SELECT 1 + ABS(CHECKSUM(pp.pk, 2301)) % 3 AS NLineas, ABS(CHECKSUM(pp.pk, 2401)) AS Base) c
CROSS APPLY (SELECT CASE WHEN pp.UsaDisp = 1 THEN @nDisp ELSE @nAll END AS N, CASE WHEN pp.UsaDisp = 1 THEN @stDisp ELSE @stAll END AS St) np
CROSS APPLY (SELECT CASE WHEN k.i > 1 AND ABS(CHECKSUM(pp.pk, k.i, 2501)) % 11 = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Anula) x
JOIN (SELECT 1 AS Disp, rn, IdProducto, Precio FROM #ProdDisp UNION ALL SELECT 0, rn, IdProducto, Precio FROM #ProdAll) p
  ON p.Disp = pp.UsaDisp AND p.rn = 1 + (c.Base + np.St * k.i) % np.N
WHERE k.i <= c.NLineas;

-- 5.6 Total de cada venta = suma de las lineas activas
UPDATE v SET Total = ISNULL((SELECT SUM(d.Subtotal) FROM dbo.DetalleVenta d WHERE d.IdVenta = v.IdVenta AND d.Anulado = 0), 0)
FROM dbo.Venta v WHERE v.IdVenta IN (SELECT IdVenta FROM #MapV);

COMMIT TRANSACTION;

/* ============================================================
   6. RESUMEN
   ============================================================ */
PRINT '--- Sembrado ---';
DECLARE @cR INT, @cV INT, @cP INT, @cL INT;
SELECT @cR = COUNT(*) FROM #Res;
SELECT @cV = COUNT(*) FROM #VentaPlan;
SELECT @cP = COUNT(*) FROM #PedPlan;
SELECT @cL = COUNT(*) FROM dbo.DetalleVenta d WHERE d.IdVenta IN (SELECT IdVenta FROM #MapV);
PRINT 'Reservas: ' + CAST(@cR AS VARCHAR(10)) + ' | Ventas: ' + CAST(@cV AS VARCHAR(10)) + ' | Pedidos: ' + CAST(@cP AS VARCHAR(10)) + ' | Lineas: ' + CAST(@cL AS VARCHAR(10));

SELECT r.Estado, COUNT(*) AS Reservas,
       SUM(CASE WHEN r.EsWalkIn = 1 THEN 1 ELSE 0 END) AS WalkIn,
       SUM(CASE WHEN r.Mesa2 IS NOT NULL THEN 1 ELSE 0 END) AS DosMesas,
       MIN(r.Fecha) AS Desde, MAX(r.Fecha) AS Hasta
FROM #Res r GROUP BY r.Estado ORDER BY r.Estado;

SELECT v.Estado, v.TipoVenta AS Tipo, COUNT(*) AS Ventas, SUM(v.Total) AS Total
FROM dbo.Venta v WHERE v.IdVenta IN (SELECT IdVenta FROM #MapV) GROUP BY v.Estado, v.TipoVenta ORDER BY v.Estado, v.TipoVenta;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: no se sembro nada (se deshizo la transaccion).';
    THROW;
END CATCH
GO

-- Reactiva la ejecucion si la seccion 0 la habia detenido (no afecta si no se detuvo)
SET NOEXEC OFF;
GO
