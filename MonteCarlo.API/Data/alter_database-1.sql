/* ============================================================
   ALTER - Automatizacion de asignacion de mesas y walk-ins
   Ejecutar sobre una base de datos MonteCarlo ya existente.

   ATENCION: este script NO es idempotente. Dropea y recrea
   ConfiguracionSistema y las tablas que dependen de Reserva, por
   lo que se pierden TODAS las reservas, asignaciones de mesa,
   notificaciones y ventas (con su detalle) que existan, y los
   valores de configuracion. Solo la seccion 3 se puede
   re-ejecutar sin efecto.

   Contenido:
   1.   ConfiguracionSistema: drop + create + seed (5 claves, con
        Etiqueta/Descripcion para la pantalla de Parametros) con
        los valores de la base de desarrollo.
   2.   Reconstruccion con el esquema actual de database.sql:
        Mesa (con PosX/PosY), Reserva (CodigoReserva nullable,
        EsWalkIn, sin IdMesa; indice unico filtrado), ReservaMesa
        (N*N), Notificacion, Venta y DetalleVenta, mas el seed de
        5 mesas.
   3.   Cliente centinela "Walk-in" y catalogos base
        (EstadoReserva, MetodoPago) si faltan.
   ============================================================ */

USE MonteCarlo;
GO

/* ------------------------------------------------------------
   1. ConfiguracionSistema se reconstruye (drop + create + seed)
      con el esquema de database.sql (Etiqueta y Descripcion para
      la pantalla dinamica de Parametros) y los valores vigentes en
      la base de desarrollo. Ninguna tabla la referencia, asi que
      se puede dropear sin tocar otras. Se pierden los valores que
      hubiera en la tabla; FechaModificacion queda en la fecha
      actual e IdUsuarioModificacion en NULL.

      - UmbralConfirmacionAutomatica (HU-CFG-001)
      - DuracionReservaMinutos (HU-CFG-006): solapamiento entre
        reservas y disponibilidad del calendario.
      - VentanaAlertaReservaProximaMinutos: bloqueo de asignacion
        de walk-ins ante una reserva proxima.
      - AnticipacionRecordatorioHoras (HU-CFG-006 / HU-NOT-003)
      - IntervaloCalendarioDisponibilidadMinutos (HU-RES-002)
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.ConfiguracionSistema', 'U') IS NOT NULL DROP TABLE dbo.ConfiguracionSistema;
GO

CREATE TABLE dbo.ConfiguracionSistema (
    Clave                  NVARCHAR(50)  NOT NULL,
    Valor                  NVARCHAR(200) NOT NULL,
    Etiqueta               NVARCHAR(150) NULL, -- nombre amigable para la pantalla de Parametros; si es NULL se muestra la Clave
    Descripcion            NVARCHAR(300) NULL, -- que almacena y para que se usa, mostrado como texto de ayuda bajo el input
    FechaModificacion      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioModificacion  INT           NULL,
    CONSTRAINT PK_ConfiguracionSistema PRIMARY KEY (Clave),
    CONSTRAINT FK_ConfiguracionSistema_Usuario
        FOREIGN KEY (IdUsuarioModificacion) REFERENCES dbo.Usuario (IdUsuario)
);
GO

INSERT INTO dbo.ConfiguracionSistema (Clave, Valor, Etiqueta, Descripcion) VALUES
    ('UmbralConfirmacionAutomatica', '10',
        'Umbral de confirmación automática',
        'Cantidad de personas a partir de la cual una reserva nueva queda Pendiente de revisión en vez de confirmarse sola.'),
    ('DuracionReservaMinutos', '90',
        'Duración de reserva (minutos)',
        'Tiempo estimado que ocupa una reserva en su mesa. Se usa para detectar solapamiento entre reservas y para el cálculo de disponibilidad del calendario.'),
    ('VentanaAlertaReservaProximaMinutos', '60',
        'Ventana de alerta de reserva próxima (minutos)',
        'Minutos antes de una reserva confirmada en los que la mesa deja de poder asignarse a un walk-in sin reserva. Independiente de la duración de reserva.'),
    ('AnticipacionRecordatorioHoras', '24',
        'Anticipación del recordatorio (horas)',
        'Horas antes de la reserva en que se envía el recordatorio automático al cliente.'),
    ('IntervaloCalendarioDisponibilidadMinutos', '30',
        'Intervalo del calendario de disponibilidad (minutos)',
        'Cada cuántos minutos se muestra un horario de llegada seleccionable en el calendario de disponibilidad.');
GO

/* ------------------------------------------------------------
   2. Reconstruccion de Mesa y de las tablas que dependen de
      Reserva (orden inverso a las dependencias). Se recrean
      identicas a database.sql, en lugar de migrar con ALTERs.
      Se pierden los datos de: DetalleVenta, Venta, Notificacion,
      ReservaMesa, Reserva y Mesa.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.DetalleVenta', 'U') IS NOT NULL DROP TABLE dbo.DetalleVenta;
IF OBJECT_ID('dbo.Venta', 'U') IS NOT NULL DROP TABLE dbo.Venta;
IF OBJECT_ID('dbo.Notificacion', 'U') IS NOT NULL DROP TABLE dbo.Notificacion;
IF OBJECT_ID('dbo.ReservaMesa', 'U') IS NOT NULL DROP TABLE dbo.ReservaMesa;
IF OBJECT_ID('dbo.Reserva', 'U') IS NOT NULL DROP TABLE dbo.Reserva;
IF OBJECT_ID('dbo.Mesa', 'U') IS NOT NULL DROP TABLE dbo.Mesa;
GO

/* ------------------------------------------------------------
   Entidad: Mesa
   ------------------------------------------------------------ */
CREATE TABLE dbo.Mesa (
    IdMesa     INT IDENTITY(1,1) NOT NULL,
    NumeroMesa INT           NOT NULL,
    Capacidad  INT           NOT NULL,
    Estado     NVARCHAR(20)  NOT NULL DEFAULT 'Disponible',
    PosX       INT           NOT NULL DEFAULT 0, -- posicion en el canvas del editor visual de mesas
    PosY       INT           NOT NULL DEFAULT 0,
    CONSTRAINT PK_Mesa PRIMARY KEY (IdMesa),
    CONSTRAINT UQ_Mesa_Numero UNIQUE (NumeroMesa),
    CONSTRAINT CK_Mesa_Capacidad CHECK (Capacidad > 0)
);
GO

/* ------------------------------------------------------------
   Entidad central: Reserva
   HU-RES-004/005: CodigoReserva es la llave publica de
        consulta/cancelacion sin necesidad de login.
   HU-MES-005/006: RequiereAsignacionManual marca las reservas
        que ningun mesa individual pudo cubrir automaticamente.
   HU-OPE-003: FechaAtencion registra cuando el cliente fue
        atendido.
   HU-RES-006 / HU-CFG-003: MotivoCancelacion e IdCierreEventual
        permiten distinguir cancelacion por cliente, por
        administrador, por expiracion automatica o por cierre
        administrativo (y en este ultimo caso, cual).
   Nota: ya no tiene IdMesa. La mesa (o mesas, si son 2
        combinadas) se asigna via la tabla ReservaMesa (N*N).
   HU-OPE-004: EsWalkIn marca un cliente sentado sin reserva
        previa; CodigoReserva queda NULL (indice unico filtrado)
        e IdCliente apunta al cliente centinela "Walk-in".
   ------------------------------------------------------------ */
CREATE TABLE dbo.Reserva (
    IdReserva               INT IDENTITY(1,1) NOT NULL,
    CodigoReserva           NVARCHAR(10)  NULL, -- codigo unico generado por la aplicacion; NULL si EsWalkIn
    IdCliente               INT           NOT NULL,
    IdEstadoReserva         INT           NOT NULL,
    FechaReserva            DATE          NOT NULL,
    HoraReserva             TIME          NOT NULL,
    CantidadPersonas        INT           NOT NULL,
    RequiereAsignacionManual BIT          NOT NULL DEFAULT 0,
    EsWalkIn                BIT           NOT NULL DEFAULT 0,
    FechaAtencion           DATETIME2     NULL,
    MotivoCancelacion       NVARCHAR(100) NULL, -- ej: 'Cancelacion por cliente', 'Cancelacion administrativa', 'Cierre administrativo', 'Expiracion automatica', 'Liberada sin consumo'
    IdCierreEventual        INT           NULL, -- solo si MotivoCancelacion = 'Cierre administrativo'
    Observaciones           NVARCHAR(300) NULL,
    FechaCreacion           DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Reserva PRIMARY KEY (IdReserva),
    CONSTRAINT FK_Reserva_Cliente
        FOREIGN KEY (IdCliente) REFERENCES dbo.Cliente (IdCliente),
    CONSTRAINT FK_Reserva_EstadoReserva
        FOREIGN KEY (IdEstadoReserva) REFERENCES dbo.EstadoReserva (IdEstadoReserva),
    CONSTRAINT FK_Reserva_CierreEventual
        FOREIGN KEY (IdCierreEventual) REFERENCES dbo.CierreEventual (IdCierreEventual),
    CONSTRAINT CK_Reserva_CantidadPersonas CHECK (CantidadPersonas > 0)
);
GO

CREATE UNIQUE INDEX UQ_Reserva_Codigo ON dbo.Reserva (CodigoReserva) WHERE CodigoReserva IS NOT NULL;
GO

/* ------------------------------------------------------------
   Entidad: ReservaMesa (relacion N*N Reserva-Mesa)
   Una reserva usa 1 mesa (caso normal) o 2 mesas adyacentes
   combinadas (grupo que no cabe en una sola mesa). Es la unica
   fuente de verdad para "que mesa(s) tiene esta reserva" -- no
   existe una columna IdMesa en Reserva.
   ------------------------------------------------------------ */
CREATE TABLE dbo.ReservaMesa (
    IdReserva  INT NOT NULL,
    IdMesa     INT NOT NULL,
    CONSTRAINT PK_ReservaMesa PRIMARY KEY (IdReserva, IdMesa),
    CONSTRAINT FK_ReservaMesa_Reserva
        FOREIGN KEY (IdReserva) REFERENCES dbo.Reserva (IdReserva),
    CONSTRAINT FK_ReservaMesa_Mesa
        FOREIGN KEY (IdMesa) REFERENCES dbo.Mesa (IdMesa)
);
GO

/* ------------------------------------------------------------
   Entidad: Notificacion
   ------------------------------------------------------------ */
CREATE TABLE dbo.Notificacion (
    IdNotificacion    INT IDENTITY(1,1) NOT NULL,
    IdReserva         INT           NOT NULL,
    TipoNotificacion  NVARCHAR(30)  NOT NULL, -- Confirmacion / Cancelacion / Recordatorio
    Canal             NVARCHAR(20)  NOT NULL, -- Email / SMS
    Destinatario      NVARCHAR(120) NOT NULL,
    Mensaje           NVARCHAR(500) NULL,
    FechaCreacion     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    FechaEnvio        DATETIME2     NULL,
    EstadoEnvio       NVARCHAR(20)  NOT NULL DEFAULT 'Pendiente', -- Pendiente/Enviado/Fallido
    DetalleError      NVARCHAR(300) NULL,
    CONSTRAINT PK_Notificacion PRIMARY KEY (IdNotificacion),
    CONSTRAINT FK_Notificacion_Reserva
        FOREIGN KEY (IdReserva) REFERENCES dbo.Reserva (IdReserva)
);
GO

/* ------------------------------------------------------------
   Entidad: Venta (HU-CON-002 consumo en mesa / HU-CON-003 takeout)
   IdReserva NOT NULL solo para TipoVenta = 'Consumo'.
   IdMetodoPago puede ser NULL unicamente si Total = 0 (cierre
   de mesa sin consumo, HU-CON-002 escenario 2); esa regla se
   valida en la capa de aplicacion.
   ------------------------------------------------------------ */
CREATE TABLE dbo.Venta (
    IdVenta            INT IDENTITY(1,1) NOT NULL,
    TipoVenta          NVARCHAR(20)   NOT NULL, -- 'Consumo' / 'Takeout'
    IdReserva          INT            NULL,
    IdMetodoPago       INT            NULL,
    Total              DECIMAL(10,2)  NOT NULL DEFAULT 0,
    IdUsuarioRegistro  INT            NOT NULL,
    FechaVenta         DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Venta PRIMARY KEY (IdVenta),
    CONSTRAINT CK_Venta_Tipo CHECK (TipoVenta IN ('Consumo', 'Takeout')),
    CONSTRAINT CK_Venta_Total CHECK (Total >= 0),
    CONSTRAINT CK_Venta_TipoReserva CHECK (
        (TipoVenta = 'Consumo'  AND IdReserva IS NOT NULL) OR
        (TipoVenta = 'Takeout'  AND IdReserva IS NULL)
    ),
    CONSTRAINT FK_Venta_Reserva
        FOREIGN KEY (IdReserva) REFERENCES dbo.Reserva (IdReserva),
    CONSTRAINT FK_Venta_MetodoPago
        FOREIGN KEY (IdMetodoPago) REFERENCES dbo.MetodoPago (IdMetodoPago),
    CONSTRAINT FK_Venta_Usuario
        FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuario (IdUsuario)
);
GO

/* ------------------------------------------------------------
   Entidad: DetalleVenta
   PrecioUnitario es una copia (snapshot) del precio de
   Producto al momento de la venta: si el precio del catalogo
   cambia despues, las ventas historicas no se alteran.
   ------------------------------------------------------------ */
CREATE TABLE dbo.DetalleVenta (
    IdDetalleVenta  INT IDENTITY(1,1) NOT NULL,
    IdVenta         INT           NOT NULL,
    IdProducto      INT           NOT NULL,
    Cantidad        INT           NOT NULL,
    PrecioUnitario  DECIMAL(10,2) NOT NULL, -- precio historico, independiente de Producto.Precio
    Subtotal        AS (Cantidad * PrecioUnitario) PERSISTED,
    CONSTRAINT PK_DetalleVenta PRIMARY KEY (IdDetalleVenta),
    CONSTRAINT CK_DetalleVenta_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_DetalleVenta_Precio CHECK (PrecioUnitario >= 0),
    CONSTRAINT FK_DetalleVenta_Venta
        FOREIGN KEY (IdVenta) REFERENCES dbo.Venta (IdVenta),
    CONSTRAINT FK_DetalleVenta_Producto
        FOREIGN KEY (IdProducto) REFERENCES dbo.Producto (IdProducto)
);
GO

CREATE INDEX IX_Reserva_FechaHora ON dbo.Reserva (FechaReserva, HoraReserva);
CREATE INDEX IX_ReservaMesa_Mesa ON dbo.ReservaMesa (IdMesa);
CREATE INDEX IX_Notificacion_Reserva ON dbo.Notificacion (IdReserva);
CREATE INDEX IX_Venta_Fecha ON dbo.Venta (FechaVenta);
CREATE INDEX IX_Venta_Tipo ON dbo.Venta (TipoVenta);
CREATE INDEX IX_DetalleVenta_Venta ON dbo.DetalleVenta (IdVenta);
CREATE INDEX IX_DetalleVenta_Producto ON dbo.DetalleVenta (IdProducto);
GO

/* Seed de mesas (misma distribucion que database.sql). */
INSERT INTO dbo.Mesa (NumeroMesa, Capacidad, Estado, PosX, PosY) VALUES
    (1, 2, 'Disponible', 395, 256),
    (2, 2, 'Disponible', 230, 352),
    (3, 4, 'Disponible', 383,  80),
    (4, 4, 'Disponible', 623, 166),
    (5, 9, 'Disponible', 952, 342);
GO

/* ------------------------------------------------------------
   3. Cliente centinela para walk-ins (HU-OPE-004) y catalogos
      base de database.sql (EstadoReserva y MetodoPago), solo si
      faltan. No se toca la cuenta admin: es un dato de ambiente.
   ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM dbo.Cliente WHERE Nombre = 'Walk-in' AND Apellido = 'Sin reserva')
    INSERT INTO dbo.Cliente (Nombre, Apellido, Telefono) VALUES ('Walk-in', 'Sin reserva', '00000000');
GO

INSERT INTO dbo.EstadoReserva (Nombre, Descripcion)
SELECT v.Nombre, v.Descripcion
FROM (VALUES
    ('Pendiente',  'Reserva registrada, en espera de revision administrativa por superar el umbral configurado'),
    ('Confirmada', 'Reserva confirmada, con mesa asignada'),
    ('Cancelada',  'Reserva cancelada por el cliente, el administrador o un cierre administrativo'),
    ('Finalizada', 'Cliente atendido, reserva completada'),
    ('No Show',    'Cliente no se presento en el horario reservado')
) AS v (Nombre, Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstadoReserva e WHERE e.Nombre = v.Nombre);
GO

INSERT INTO dbo.MetodoPago (Nombre)
SELECT v.Nombre
FROM (VALUES ('Efectivo'), ('Tarjeta'), ('SINPE')) AS v (Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.MetodoPago m WHERE m.Nombre = v.Nombre);
GO

