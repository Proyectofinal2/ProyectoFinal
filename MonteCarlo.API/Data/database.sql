/* ============================================================
   Sistema de Gestion de Reservas - Mirador Monte Carlo
   Script de creacion de esquema (DER) - VERSION 2
   Motor: SQL Server
   Uso: Ejecutar completo. Luego, en SSMS -> Database Diagrams
        -> New Database Diagram -> agregar todas las tablas,
        y la herramienta genera el DER automaticamente a partir
        de las llaves foraneas aqui definidas.
   Nota: Para modificar el modelo, edita este archivo y
         vuelve a ejecutarlo (usa DROP + CREATE controlado).

   ------------------------------------------------------------
   CAMBIOS RESPECTO A LA VERSION 1 (basados en historias de
   usuario corregidas + documento de flujo):

   1. Reserva: se agregan CodigoReserva (busqueda sin login),
      RequiereAsignacionManual (HU-MES-005/006), FechaAtencion
      (HU-OPE-003), MotivoCancelacion e IdCierreEventual
      (HU-RES-006, HU-CFG-003, HU-REP-002).
   2. Usuario: se agregan IntentosFallidos y FechaBloqueoHasta
      (HU-AUT-001) para bloqueo temporal por intentos fallidos.
   3. Nueva tabla TokenRecuperacionContrasena (HU-AUT-003).
   4. Nueva tabla ConfiguracionSistema, clave/valor con
      auditoria de quien y cuando modifico (HU-CFG-001). Ya no
      se hardcodea el umbral de 4 personas.
   5. Nuevas tablas CierreFijo y CierreEventual (HU-CFG-002 y
      HU-CFG-003).
   6. Nueva tabla HistorialCierre: log de auditoria separado
      (registrar/eliminar) para cierres fijos y eventuales,
      con administrador responsable, fecha y motivo
      (HU-CFG-004).
   7. Nuevas tablas Producto, MetodoPago, Venta y DetalleVenta
      para el modulo de consumo y ventas (HU-CON-001/002/003),
      incluyendo precio historico por venta (no referencia el
      precio vigente del catalogo).
   8. El chatbot (modulo CHB) reutiliza el motor de reservas
      existente y no requiere tablas propias.
   ============================================================ */

IF DB_ID('MonteCarlo') IS NULL
    CREATE DATABASE MonteCarlo;
GO

USE MonteCarlo;
GO

/* ------------------------------------------------------------
   Limpieza (orden inverso a las dependencias) para permitir
   re-ejecutar el script cuantas veces sea necesario
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.DetalleVenta', 'U') IS NOT NULL DROP TABLE dbo.DetalleVenta;
IF OBJECT_ID('dbo.Venta', 'U') IS NOT NULL DROP TABLE dbo.Venta;
IF OBJECT_ID('dbo.TokenRecuperacionContrasena', 'U') IS NOT NULL DROP TABLE dbo.TokenRecuperacionContrasena;
IF OBJECT_ID('dbo.ConfiguracionSistema', 'U') IS NOT NULL DROP TABLE dbo.ConfiguracionSistema;
IF OBJECT_ID('dbo.HistorialCierre', 'U') IS NOT NULL DROP TABLE dbo.HistorialCierre;
IF OBJECT_ID('dbo.Notificacion', 'U') IS NOT NULL DROP TABLE dbo.Notificacion;
IF OBJECT_ID('dbo.Reserva', 'U') IS NOT NULL DROP TABLE dbo.Reserva;
IF OBJECT_ID('dbo.CierreEventual', 'U') IS NOT NULL DROP TABLE dbo.CierreEventual;
IF OBJECT_ID('dbo.CierreFijo', 'U') IS NOT NULL DROP TABLE dbo.CierreFijo;
IF OBJECT_ID('dbo.Producto', 'U') IS NOT NULL DROP TABLE dbo.Producto;
IF OBJECT_ID('dbo.MetodoPago', 'U') IS NOT NULL DROP TABLE dbo.MetodoPago;
IF OBJECT_ID('dbo.HorarioOperacion', 'U') IS NOT NULL DROP TABLE dbo.HorarioOperacion;
IF OBJECT_ID('dbo.Usuario', 'U') IS NOT NULL DROP TABLE dbo.Usuario;
IF OBJECT_ID('dbo.Mesa', 'U') IS NOT NULL DROP TABLE dbo.Mesa;
IF OBJECT_ID('dbo.Cliente', 'U') IS NOT NULL DROP TABLE dbo.Cliente;
IF OBJECT_ID('dbo.EstadoReserva', 'U') IS NOT NULL DROP TABLE dbo.EstadoReserva;
GO

/* ------------------------------------------------------------
   Catalogo: EstadoReserva
   (Pendiente, Confirmada, Cancelada, Finalizada, No Show)
   ------------------------------------------------------------ */
CREATE TABLE dbo.EstadoReserva (
    IdEstadoReserva INT IDENTITY(1,1) NOT NULL,
    Nombre          NVARCHAR(30)  NOT NULL,
    Descripcion     NVARCHAR(200) NULL,
    CONSTRAINT PK_EstadoReserva PRIMARY KEY (IdEstadoReserva),
    CONSTRAINT UQ_EstadoReserva_Nombre UNIQUE (Nombre)
);
GO

/* ------------------------------------------------------------
   Entidad: Cliente
   Datos capturados al reservar (sin cuenta / sin login)
   ------------------------------------------------------------ */
CREATE TABLE dbo.Cliente (
    IdCliente         INT IDENTITY(1,1) NOT NULL,
    Nombre            NVARCHAR(80)  NOT NULL,
    Apellido          NVARCHAR(80)  NULL,
    Telefono          NVARCHAR(20)  NOT NULL,
    CorreoElectronico NVARCHAR(120) NULL,
    FechaCreacion     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Cliente PRIMARY KEY (IdCliente)
);
GO

/* ------------------------------------------------------------
   Entidad: Mesa
   ------------------------------------------------------------ */
CREATE TABLE dbo.Mesa (
    IdMesa     INT IDENTITY(1,1) NOT NULL,
    NumeroMesa INT           NOT NULL,
    Capacidad  INT           NOT NULL,
    Estado     NVARCHAR(20)  NOT NULL DEFAULT 'Disponible',
    CONSTRAINT PK_Mesa PRIMARY KEY (IdMesa),
    CONSTRAINT UQ_Mesa_Numero UNIQUE (NumeroMesa),
    CONSTRAINT CK_Mesa_Capacidad CHECK (Capacidad > 0)
);
GO

/* ------------------------------------------------------------
   Entidad: Usuario (Administrador del modulo administrativo)
   Independiente del flujo de reservas
   HU-AUT-001: intentos fallidos + bloqueo temporal
   HU-AUT-003: recuperacion de contrasena (ver tabla aparte)
   HU-AUT-005/006: Rol distingue General (puede crear/desactivar
        otras cuentas) de Regular (no puede)
   ------------------------------------------------------------ */
CREATE TABLE dbo.Usuario (
    IdUsuario          INT IDENTITY(1,1) NOT NULL,
    NombreUsuario      NVARCHAR(50)  NOT NULL,
    NombreCompleto     NVARCHAR(120) NOT NULL,
    CorreoElectronico  NVARCHAR(120) NOT NULL,
    Rol                NVARCHAR(20)  NOT NULL DEFAULT 'Regular', -- 'General' / 'Regular'
    ContrasenaHash     NVARCHAR(255) NOT NULL,
    Activo             BIT           NOT NULL DEFAULT 1,
    IntentosFallidos   INT           NOT NULL DEFAULT 0,
    FechaBloqueoHasta  DATETIME2     NULL, -- NULL = no bloqueado
    FechaCreacion      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Usuario PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT UQ_Usuario_Correo UNIQUE (CorreoElectronico),
    CONSTRAINT CK_Usuario_Rol CHECK (Rol IN ('General', 'Regular'))
);
GO

/* ------------------------------------------------------------
   Catalogo: HorarioOperacion
   Define franjas de atencion por dia de semana (HU-CFG-005)
   ------------------------------------------------------------ */
CREATE TABLE dbo.HorarioOperacion (
    IdHorarioOperacion INT IDENTITY(1,1) NOT NULL,
    DiaSemana          TINYINT NOT NULL, -- 0=Domingo ... 6=Sabado
    HoraApertura       TIME    NOT NULL,
    HoraCierre         TIME    NOT NULL,
    Activo             BIT     NOT NULL DEFAULT 1,
    CONSTRAINT PK_HorarioOperacion PRIMARY KEY (IdHorarioOperacion),
    CONSTRAINT CK_HorarioOperacion_Rango CHECK (HoraCierre > HoraApertura),
    CONSTRAINT CK_HorarioOperacion_Dia CHECK (DiaSemana BETWEEN 0 AND 6)
);
GO

/* ------------------------------------------------------------
   Catalogo: MetodoPago (HU-CON-002 / HU-CON-003 / HU-REP-003)
   ------------------------------------------------------------ */
CREATE TABLE dbo.MetodoPago (
    IdMetodoPago INT IDENTITY(1,1) NOT NULL,
    Nombre       NVARCHAR(30) NOT NULL,
    Activo       BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_MetodoPago PRIMARY KEY (IdMetodoPago),
    CONSTRAINT UQ_MetodoPago_Nombre UNIQUE (Nombre)
);
GO

/* ------------------------------------------------------------
   Entidad: Producto (HU-CON-001)
   Catalogo de productos vendibles (consumo en mesa o takeout)
   ------------------------------------------------------------ */
CREATE TABLE dbo.Producto (
    IdProducto    INT IDENTITY(1,1) NOT NULL,
    Nombre        NVARCHAR(100)  NOT NULL,
    Categoria     NVARCHAR(50)   NULL,
    Precio        DECIMAL(10,2)  NOT NULL,
    Disponible    BIT            NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Producto PRIMARY KEY (IdProducto),
    CONSTRAINT CK_Producto_Precio CHECK (Precio >= 0)
);
GO

/* ------------------------------------------------------------
   Entidad: CierreFijo (HU-CFG-002)
   Dias de la semana marcados como cierre recurrente.
   Al eliminarse, la fila se borra; la accion queda registrada
   en HistorialCierre.
   ------------------------------------------------------------ */
CREATE TABLE dbo.CierreFijo (
    IdCierreFijo       INT IDENTITY(1,1) NOT NULL,
    DiaSemana          TINYINT   NOT NULL, -- 0=Domingo ... 6=Sabado
    FechaCreacion      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioCreacion  INT       NOT NULL,
    CONSTRAINT PK_CierreFijo PRIMARY KEY (IdCierreFijo),
    CONSTRAINT UQ_CierreFijo_Dia UNIQUE (DiaSemana),
    CONSTRAINT CK_CierreFijo_Dia CHECK (DiaSemana BETWEEN 0 AND 6),
    CONSTRAINT FK_CierreFijo_Usuario
        FOREIGN KEY (IdUsuarioCreacion) REFERENCES dbo.Usuario (IdUsuario)
);
GO

/* ------------------------------------------------------------
   Entidad: CierreEventual (HU-CFG-003)
   Fechas puntuales cerradas (evento, mantenimiento, etc.).
   No se definio flujo de eliminacion en las HU, por lo que
   estas filas se conservan siempre (necesario para el reporte
   de impacto de HU-REP-002, que cuenta cancelaciones en
   cascada por cierre).
   ------------------------------------------------------------ */
CREATE TABLE dbo.CierreEventual (
    IdCierreEventual   INT IDENTITY(1,1) NOT NULL,
    Fecha              DATE          NOT NULL,
    Motivo             NVARCHAR(300) NOT NULL,
    FechaCreacion      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioCreacion  INT           NOT NULL,
    CONSTRAINT PK_CierreEventual PRIMARY KEY (IdCierreEventual),
    CONSTRAINT UQ_CierreEventual_Fecha UNIQUE (Fecha),
    CONSTRAINT FK_CierreEventual_Usuario
        FOREIGN KEY (IdUsuarioCreacion) REFERENCES dbo.Usuario (IdUsuario)
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
   ------------------------------------------------------------ */
CREATE TABLE dbo.Reserva (
    IdReserva               INT IDENTITY(1,1) NOT NULL,
    CodigoReserva           NVARCHAR(10)  NOT NULL, -- codigo unico generado por la aplicacion
    IdCliente               INT           NOT NULL,
    IdMesa                  INT           NULL, -- NULL mientras esta Pendiente o requiere asignacion manual
    IdEstadoReserva         INT           NOT NULL,
    FechaReserva            DATE          NOT NULL,
    HoraReserva             TIME          NOT NULL,
    CantidadPersonas        INT           NOT NULL,
    RequiereAsignacionManual BIT          NOT NULL DEFAULT 0,
    FechaAtencion           DATETIME2     NULL,
    MotivoCancelacion       NVARCHAR(100) NULL, -- ej: 'Cancelacion por cliente', 'Cancelacion administrativa', 'Cierre administrativo', 'Expiracion automatica'
    IdCierreEventual        INT           NULL, -- solo si MotivoCancelacion = 'Cierre administrativo'
    Observaciones           NVARCHAR(300) NULL,
    FechaCreacion           DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Reserva PRIMARY KEY (IdReserva),
    CONSTRAINT UQ_Reserva_Codigo UNIQUE (CodigoReserva),
    CONSTRAINT FK_Reserva_Cliente
        FOREIGN KEY (IdCliente) REFERENCES dbo.Cliente (IdCliente),
    CONSTRAINT FK_Reserva_Mesa
        FOREIGN KEY (IdMesa) REFERENCES dbo.Mesa (IdMesa),
    CONSTRAINT FK_Reserva_EstadoReserva
        FOREIGN KEY (IdEstadoReserva) REFERENCES dbo.EstadoReserva (IdEstadoReserva),
    CONSTRAINT FK_Reserva_CierreEventual
        FOREIGN KEY (IdCierreEventual) REFERENCES dbo.CierreEventual (IdCierreEventual),
    CONSTRAINT CK_Reserva_CantidadPersonas CHECK (CantidadPersonas > 0)
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
   Entidad: HistorialCierre (HU-CFG-004)
   Log de auditoria de acciones sobre cierres fijos y
   eventuales. Independiente de CierreFijo/CierreEventual para
   que la accion "Eliminado" quede registrada aun despues de
   borrar la fila de CierreFijo.
   ------------------------------------------------------------ */
CREATE TABLE dbo.HistorialCierre (
    IdHistorialCierre INT IDENTITY(1,1) NOT NULL,
    TipoCierre         NVARCHAR(10)  NOT NULL, -- 'Fijo' / 'Eventual'
    Accion              NVARCHAR(20)  NOT NULL, -- 'Registrado' / 'Eliminado'
    Fecha               DATETIME2     NOT NULL DEFAULT SYSDATETIME(), -- fecha/hora en que se realizo la accion
    IdUsuario           INT           NOT NULL, -- administrador responsable
    Motivo              NVARCHAR(300) NULL, -- aplica principalmente a cierres eventuales
    DiaSemana           TINYINT       NULL, -- aplica si TipoCierre = 'Fijo'
    FechaCierre         DATE          NULL, -- aplica si TipoCierre = 'Eventual'
    IdCierreEventual    INT           NULL, -- referencia opcional, solo para 'Eventual'
    CONSTRAINT PK_HistorialCierre PRIMARY KEY (IdHistorialCierre),
    CONSTRAINT CK_HistorialCierre_Tipo CHECK (TipoCierre IN ('Fijo', 'Eventual')),
    CONSTRAINT CK_HistorialCierre_Accion CHECK (Accion IN ('Registrado', 'Eliminado')),
    CONSTRAINT FK_HistorialCierre_Usuario
        FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario),
    CONSTRAINT FK_HistorialCierre_CierreEventual
        FOREIGN KEY (IdCierreEventual) REFERENCES dbo.CierreEventual (IdCierreEventual)
);
GO

/* ------------------------------------------------------------
   Entidad: ConfiguracionSistema (HU-CFG-001)
   Tabla clave/valor generica. Hoy solo se usa para el umbral
   de confirmacion automatica, pero queda lista para futuros
   parametros sin requerir cambios de esquema.
   ------------------------------------------------------------ */
CREATE TABLE dbo.ConfiguracionSistema (
    Clave                  NVARCHAR(50)  NOT NULL,
    Valor                  NVARCHAR(200) NOT NULL,
    FechaModificacion      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioModificacion  INT           NULL,
    CONSTRAINT PK_ConfiguracionSistema PRIMARY KEY (Clave),
    CONSTRAINT FK_ConfiguracionSistema_Usuario
        FOREIGN KEY (IdUsuarioModificacion) REFERENCES dbo.Usuario (IdUsuario)
);
GO

/* ------------------------------------------------------------
   Entidad: TokenRecuperacionContrasena (HU-AUT-003)
   ------------------------------------------------------------ */
CREATE TABLE dbo.TokenRecuperacionContrasena (
    IdToken           INT IDENTITY(1,1) NOT NULL,
    IdUsuario         INT           NOT NULL,
    TokenHash         NVARCHAR(255) NOT NULL, -- nunca se guarda el token en texto plano
    FechaCreacion     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    FechaExpiracion   DATETIME2     NOT NULL,
    Utilizado         BIT           NOT NULL DEFAULT 0,
    FechaUtilizado     DATETIME2     NULL,
    CONSTRAINT PK_TokenRecuperacionContrasena PRIMARY KEY (IdToken),
    CONSTRAINT UQ_TokenRecuperacion_Hash UNIQUE (TokenHash),
    CONSTRAINT FK_TokenRecuperacion_Usuario
        FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
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

/* ------------------------------------------------------------
   Indices de apoyo para las consultas mas frecuentes
   (calendario de disponibilidad, vista operativa diaria,
   busqueda por codigo, reportes)
   ------------------------------------------------------------ */
CREATE INDEX IX_Reserva_FechaHora ON dbo.Reserva (FechaReserva, HoraReserva);
CREATE INDEX IX_Reserva_Mesa ON dbo.Reserva (IdMesa);
CREATE INDEX IX_Notificacion_Reserva ON dbo.Notificacion (IdReserva);
CREATE INDEX IX_Venta_Fecha ON dbo.Venta (FechaVenta);
CREATE INDEX IX_Venta_Tipo ON dbo.Venta (TipoVenta);
CREATE INDEX IX_DetalleVenta_Venta ON dbo.DetalleVenta (IdVenta);
CREATE INDEX IX_DetalleVenta_Producto ON dbo.DetalleVenta (IdProducto);
CREATE INDEX IX_HistorialCierre_Fecha ON dbo.HistorialCierre (Fecha);
CREATE INDEX IX_HistorialCierre_Tipo ON dbo.HistorialCierre (TipoCierre);
CREATE INDEX IX_HistorialCierre_Usuario ON dbo.HistorialCierre (IdUsuario);
GO

/* ------------------------------------------------------------
   Datos base del catalogo de estados (segun alcance aprobado).
   Ya no se hardcodea "mas de 4 personas": el umbral vive en
   ConfiguracionSistema (HU-CFG-001).
   ------------------------------------------------------------ */
INSERT INTO dbo.EstadoReserva (Nombre, Descripcion) VALUES
    ('Pendiente',  'Reserva registrada, en espera de revision administrativa por superar el umbral configurado'),
    ('Confirmada', 'Reserva confirmada, con mesa asignada'),
    ('Cancelada',  'Reserva cancelada por el cliente, el administrador o un cierre administrativo'),
    ('Finalizada', 'Cliente atendido, reserva completada'),
    ('No Show',    'Cliente no se presento en el horario reservado');
GO

/* ------------------------------------------------------------
   Datos base: metodos de pago (HU-CON-002 / HU-CON-003 / HU-REP-003)
   ------------------------------------------------------------ */
INSERT INTO dbo.MetodoPago (Nombre) VALUES
    ('Efectivo'),
    ('Tarjeta'),
    ('SINPE');
GO

/* ------------------------------------------------------------
   Datos base: umbral de confirmacion automatica (HU-CFG-001)
   Valor inicial de 4 personas, tal como indica el flujo.
   IdUsuarioModificacion queda NULL porque este valor inicial
   no fue modificado por ningun administrador.
   ------------------------------------------------------------ */
INSERT INTO dbo.ConfiguracionSistema (Clave, Valor) VALUES
    ('UmbralConfirmacionAutomatica', '4');
GO


/* ------------------------------------------------------------
   Datos base: primera cuenta de Administrador General
   (HU-AUT-006 - bootstrap). Se crea por script porque ningun
   administrador puede existir todavia para crearla desde el
   sistema.
 
   IMPORTANTE: 'CAMBIAR_ESTE_HASH' es un marcador, NO una
   contrasena en texto plano ni un hash valido. Antes de usar
   esta cuenta, la aplicacion debe generar un hash real (ej.
   bcrypt) para una contrasena definida por el equipo y
   reemplazar este valor -- nunca ejecutar este INSERT en un
   ambiente real sin hacerlo primero.
   ------------------------------------------------------------ */
INSERT INTO dbo.Usuario (NombreUsuario, NombreCompleto, CorreoElectronico, ContrasenaHash, Rol) VALUES
    ('admin', 'Administrador General', 'admin@miradormontecarlo.com', 'CAMBIAR_ESTE_HASH', 'General');
GO