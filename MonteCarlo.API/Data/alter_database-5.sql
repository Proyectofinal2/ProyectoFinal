USE MonteCarlo;
GO

/* ============================================================
   alter_database-5.sql - Ventas y cuenta de mesa (HU-CON-002 / HU-CON-003)

   Script idempotente para una base YA CREADA con database.sql
   (o con los alter anteriores). Se puede ejecutar varias veces
   y termina siempre en el mismo esquema que database.sql.

   Requisitos: tablas dbo.Venta, dbo.DetalleVenta, dbo.Usuario y
   dbo.MetodoPago (database.sql). Si faltan, el script se detiene
   sin cambiar nada.

   Cambios:
   1. Nueva tabla PedidoVenta: una ronda de pedido dentro de una
      cuenta (quien, cuando y una clave de idempotencia para que
      un doble clic no duplique el pedido).
   2. Venta: Estado (Abierta / Cobrada / Anulada), datos del cobro
      (FechaCobro, IdUsuarioCobro), datos de anulacion (HU-CON-006)
      y RowVersion (concurrencia). FechaVenta pasa a significar
      "apertura de la cuenta". Una cuenta abierta nunca cuenta como
      ingreso: los reportes filtran Estado = 'Cobrada'.
   3. DetalleVenta: cada linea pertenece a un pedido de su misma
      cuenta (FK compuesta) y puede anularse sin borrarse (queda
      quien y cuando).
   4. Las ventas que ya existian se convierten en Cobradas, cada una
      con un pedido que agrupa sus lineas.

   Nota: son cambios de esquema en varios lotes (GO) sin una
   transaccion unica. Si un lote falla, corrige la causa y vuelve
   a ejecutar el script: retoma desde donde quedo.
   ============================================================ */

/* ------------------------------------------------------------
   0. Requisitos. Si faltan tablas base, se omite el resto del
      script (SET NOEXEC) en vez de fallar a medias.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.Venta', 'U') IS NULL OR OBJECT_ID('dbo.DetalleVenta', 'U') IS NULL
   OR OBJECT_ID('dbo.Usuario', 'U') IS NULL OR OBJECT_ID('dbo.MetodoPago', 'U') IS NULL
BEGIN
    RAISERROR(N'Faltan las tablas dbo.Venta, dbo.DetalleVenta, dbo.Usuario o dbo.MetodoPago: ejecuta primero database.sql. No se aplico ningun cambio.', 16, 1);
    SET NOEXEC ON;
END
GO

/* ------------------------------------------------------------
   1. PedidoVenta
   Cada vez que se agrega una orden a la cuenta de la mesa se guarda
   un pedido (una ronda). Sin esta tabla la cuenta seria una lista
   plana de productos. Aporta:
   - Idempotencia: cada pedido lleva una clave unica generada por la
     pantalla. Si se pulsa "Agregar a la cuenta" dos veces, o el
     navegador reintenta por una falla de red, el servidor reconoce
     la clave y registra el pedido una sola vez (sin cobrar de mas).
   - Trazabilidad: quien pidio que y cuando ("Pedido 2 - 19:50"),
     para auditar la cuenta.
   - Base para una pantalla de cocina (un pedido es lo que cocina
     prepararia); hoy no se usa.
   UQ (IdPedido, IdVenta) existe solo para que DetalleVenta pueda
   apuntar a ambos y la base garantice que linea y pedido son de
   la misma cuenta.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.PedidoVenta', 'U') IS NULL
    CREATE TABLE dbo.PedidoVenta (
        IdPedido          INT IDENTITY(1,1) NOT NULL,
        IdVenta           INT              NOT NULL,
        IdUsuario         INT              NOT NULL, -- administrador que registro el pedido
        Fecha             DATETIME2        NOT NULL CONSTRAINT DF_PedidoVenta_Fecha DEFAULT SYSDATETIME(),
        ClaveIdempotencia UNIQUEIDENTIFIER NULL,     -- generada por la pantalla; evita duplicar el pedido
        Nota              NVARCHAR(200)    NULL,
        CONSTRAINT PK_PedidoVenta PRIMARY KEY (IdPedido),
        CONSTRAINT UQ_PedidoVenta_Pedido_Venta UNIQUE (IdPedido, IdVenta),
        CONSTRAINT FK_PedidoVenta_Venta
            FOREIGN KEY (IdVenta) REFERENCES dbo.Venta (IdVenta),
        CONSTRAINT FK_PedidoVenta_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_PedidoVenta_Clave' AND object_id = OBJECT_ID('dbo.PedidoVenta'))
    CREATE UNIQUE INDEX UQ_PedidoVenta_Clave ON dbo.PedidoVenta (ClaveIdempotencia) WHERE ClaveIdempotencia IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PedidoVenta_Venta' AND object_id = OBJECT_ID('dbo.PedidoVenta'))
    CREATE INDEX IX_PedidoVenta_Venta ON dbo.PedidoVenta (IdVenta);
GO

/* ------------------------------------------------------------
   2. Venta: columnas nuevas
   ------------------------------------------------------------ */
IF COL_LENGTH('dbo.Venta', 'Estado') IS NULL
    ALTER TABLE dbo.Venta ADD Estado NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.Venta', 'FechaCobro') IS NULL
    ALTER TABLE dbo.Venta ADD FechaCobro DATETIME2 NULL;
IF COL_LENGTH('dbo.Venta', 'IdUsuarioCobro') IS NULL
    ALTER TABLE dbo.Venta ADD IdUsuarioCobro INT NULL;
IF COL_LENGTH('dbo.Venta', 'MotivoAnulacion') IS NULL
    ALTER TABLE dbo.Venta ADD MotivoAnulacion NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.Venta', 'FechaAnulacion') IS NULL
    ALTER TABLE dbo.Venta ADD FechaAnulacion DATETIME2 NULL;
IF COL_LENGTH('dbo.Venta', 'IdUsuarioAnulacion') IS NULL
    ALTER TABLE dbo.Venta ADD IdUsuarioAnulacion INT NULL;
IF COL_LENGTH('dbo.Venta', 'RowVersion') IS NULL
    ALTER TABLE dbo.Venta ADD RowVersion ROWVERSION NOT NULL;
GO

/* 2.1 Ventas que ya existian: quedan Cobradas, cobradas en su fecha y por quien las registro */
UPDATE dbo.Venta
SET Estado = 'Cobrada', FechaCobro = FechaVenta, IdUsuarioCobro = IdUsuarioRegistro
WHERE Estado IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Venta') AND name = 'Estado' AND is_nullable = 1)
    ALTER TABLE dbo.Venta ALTER COLUMN Estado NVARCHAR(10) NOT NULL;
GO

/* 2.2 Claves foraneas de los usuarios de cobro y anulacion */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Venta_UsuarioCobro' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT FK_Venta_UsuarioCobro
        FOREIGN KEY (IdUsuarioCobro) REFERENCES dbo.Usuario (IdUsuario);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Venta_UsuarioAnulacion' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT FK_Venta_UsuarioAnulacion
        FOREIGN KEY (IdUsuarioAnulacion) REFERENCES dbo.Usuario (IdUsuario);
GO

/* 2.3 Reglas de negocio que garantiza la base (no solo la aplicacion).
   Antes se revisa que los datos existentes las cumplan: la regla antigua (metodo de pago
   solo puede faltar con total 0) vivia unicamente en la aplicacion. Si hay ventas que no
   la cumplen, se omite el resto del script en vez de fallar a medias. */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Cobro' AND parent_object_id = OBJECT_ID('dbo.Venta'))
   AND EXISTS (SELECT 1 FROM dbo.Venta WHERE Total > 0 AND IdMetodoPago IS NULL)
BEGIN
    RAISERROR(N'Hay ventas con total mayor que cero y sin metodo de pago. Asignales un metodo de pago y vuelve a ejecutar el script (retoma desde aqui).', 16, 1);
    SET NOEXEC ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Estado' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Estado
        CHECK (Estado IN ('Abierta', 'Cobrada', 'Anulada'));

-- Una cuenta abierta solo existe para consumo en mesa y aun no tiene metodo de pago ni cobro.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Abierta' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Abierta CHECK (
        Estado <> 'Abierta' OR
        (TipoVenta = 'Consumo' AND IdMetodoPago IS NULL AND FechaCobro IS NULL AND IdUsuarioCobro IS NULL)
    );

-- Cobrada (y Anulada, que fue cobrada antes) exige fecha y usuario de cobro y, si hay total, metodo de pago.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Cobro' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Cobro CHECK (
        Estado = 'Abierta' OR
        (FechaCobro IS NOT NULL AND IdUsuarioCobro IS NOT NULL AND (Total = 0 OR IdMetodoPago IS NOT NULL))
    );

-- Anulada exige motivo, fecha y usuario; si no, los tres quedan vacios.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Anulacion' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Anulacion CHECK (
        (Estado = 'Anulada' AND MotivoAnulacion IS NOT NULL AND FechaAnulacion IS NOT NULL AND IdUsuarioAnulacion IS NOT NULL) OR
        (Estado <> 'Anulada' AND MotivoAnulacion IS NULL AND FechaAnulacion IS NULL AND IdUsuarioAnulacion IS NULL)
    );
GO

/* 2.4 Indices.
   UQ_Venta_Reserva: una sola cuenta por reserva. Si ya hay reservas con mas de una venta de
   consumo no se crea: corrige los duplicados y vuelve a ejecutar el script. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Venta_Reserva' AND object_id = OBJECT_ID('dbo.Venta'))
BEGIN
    IF EXISTS (SELECT IdReserva FROM dbo.Venta WHERE IdReserva IS NOT NULL GROUP BY IdReserva HAVING COUNT(*) > 1)
        PRINT 'ADVERTENCIA: hay reservas con mas de una venta; no se creo UQ_Venta_Reserva. Corrige los duplicados y vuelve a ejecutar.';
    ELSE
        CREATE UNIQUE INDEX UQ_Venta_Reserva ON dbo.Venta (IdReserva) WHERE IdReserva IS NOT NULL;
END

-- Reportes de ingresos (HU-CON-004 / HU-REP-003): solo ventas cobradas, por fecha de cobro.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Venta_Cobrada_Fecha' AND object_id = OBJECT_ID('dbo.Venta'))
    CREATE INDEX IX_Venta_Cobrada_Fecha ON dbo.Venta (FechaCobro) INCLUDE (Total, TipoVenta, IdMetodoPago) WHERE Estado = 'Cobrada';
GO

/* ------------------------------------------------------------
   3. DetalleVenta: pedido y anulacion de linea
   ------------------------------------------------------------ */
IF COL_LENGTH('dbo.DetalleVenta', 'IdPedido') IS NULL
    ALTER TABLE dbo.DetalleVenta ADD IdPedido INT NULL;
IF COL_LENGTH('dbo.DetalleVenta', 'Anulado') IS NULL
    ALTER TABLE dbo.DetalleVenta ADD Anulado BIT NOT NULL CONSTRAINT DF_DetalleVenta_Anulado DEFAULT 0;
IF COL_LENGTH('dbo.DetalleVenta', 'MotivoAnulacion') IS NULL
    ALTER TABLE dbo.DetalleVenta ADD MotivoAnulacion NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.DetalleVenta', 'FechaAnulacion') IS NULL
    ALTER TABLE dbo.DetalleVenta ADD FechaAnulacion DATETIME2 NULL;
IF COL_LENGTH('dbo.DetalleVenta', 'IdUsuarioAnulacion') IS NULL
    ALTER TABLE dbo.DetalleVenta ADD IdUsuarioAnulacion INT NULL;
GO

/* 3.1 Lineas que ya existian: un pedido por venta (hecho por quien la registro, en su fecha) */
INSERT INTO dbo.PedidoVenta (IdVenta, IdUsuario, Fecha)
SELECT v.IdVenta, v.IdUsuarioRegistro, v.FechaVenta
FROM dbo.Venta v
WHERE EXISTS (SELECT 1 FROM dbo.DetalleVenta d WHERE d.IdVenta = v.IdVenta AND d.IdPedido IS NULL)
  AND NOT EXISTS (SELECT 1 FROM dbo.PedidoVenta p WHERE p.IdVenta = v.IdVenta);

UPDATE d
SET IdPedido = (SELECT MIN(p.IdPedido) FROM dbo.PedidoVenta p WHERE p.IdVenta = d.IdVenta)
FROM dbo.DetalleVenta d
WHERE d.IdPedido IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DetalleVenta') AND name = 'IdPedido' AND is_nullable = 1)
    ALTER TABLE dbo.DetalleVenta ALTER COLUMN IdPedido INT NOT NULL;
GO

/* 3.2 Claves foraneas: linea y pedido de la misma cuenta; usuario que anulo la linea */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DetalleVenta_Pedido' AND parent_object_id = OBJECT_ID('dbo.DetalleVenta'))
    ALTER TABLE dbo.DetalleVenta ADD CONSTRAINT FK_DetalleVenta_Pedido
        FOREIGN KEY (IdPedido, IdVenta) REFERENCES dbo.PedidoVenta (IdPedido, IdVenta);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DetalleVenta_UsuarioAnulacion' AND parent_object_id = OBJECT_ID('dbo.DetalleVenta'))
    ALTER TABLE dbo.DetalleVenta ADD CONSTRAINT FK_DetalleVenta_UsuarioAnulacion
        FOREIGN KEY (IdUsuarioAnulacion) REFERENCES dbo.Usuario (IdUsuario);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DetalleVenta_Anulacion' AND parent_object_id = OBJECT_ID('dbo.DetalleVenta'))
    ALTER TABLE dbo.DetalleVenta ADD CONSTRAINT CK_DetalleVenta_Anulacion CHECK (
        (Anulado = 1 AND FechaAnulacion IS NOT NULL AND IdUsuarioAnulacion IS NOT NULL) OR
        (Anulado = 0 AND MotivoAnulacion IS NULL AND FechaAnulacion IS NULL AND IdUsuarioAnulacion IS NULL)
    );
GO

/* 3.3 Indices: lineas de un pedido y lineas activas de una cuenta (total y productos) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DetalleVenta_Pedido' AND object_id = OBJECT_ID('dbo.DetalleVenta'))
    CREATE INDEX IX_DetalleVenta_Pedido ON dbo.DetalleVenta (IdPedido);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DetalleVenta_Venta_Activo' AND object_id = OBJECT_ID('dbo.DetalleVenta'))
    CREATE INDEX IX_DetalleVenta_Venta_Activo ON dbo.DetalleVenta (IdVenta) INCLUDE (Cantidad, Subtotal) WHERE Anulado = 0;
GO

-- Reactiva la ejecucion si la seccion 0 la habia detenido (no afecta si no se detuvo)
SET NOEXEC OFF;
GO
