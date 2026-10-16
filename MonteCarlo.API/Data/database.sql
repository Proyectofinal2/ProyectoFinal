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

   ------------------------------------------------------------
   CAMBIOS ADICIONALES (automatizacion de asignacion de mesas
   por combinacion):

   9. Reserva.IdMesa se elimina. La relacion Reserva-Mesa pasa
      a ser N*N via la nueva tabla ReservaMesa, ya que una
      reserva puede necesitar 1 o 2 mesas combinadas (mesas
      adyacentes). ReservaMesa es la unica fuente de verdad,
      incluso cuando la reserva usa una sola mesa.
   10. Nuevas claves en ConfiguracionSistema: DuracionReservaMinutos
      (duracion estimada de un turno, antes hardcodeada en 90
      minutos; se usa para solapamiento y disponibilidad),
      VentanaAlertaReservaProximaMinutos (ventana de bloqueo
      para no asignar un walk-in a una mesa con una reserva
      confirmada por llegar), y AnticipacionRecordatorioHoras
      (HU-CFG-006 / HU-NOT-003, cuanto antes de la reserva se
      envia el recordatorio al cliente).
   11. ConfiguracionSistema agrega Etiqueta y Descripcion: la
      pantalla de Parametros es dinamica (pinta lo que exista
      en la tabla, sin un set fijo de claves), y estas columnas
      permiten que una clave nueva traiga su propio nombre
      amigable y texto de ayuda sin tocar codigo.
   12. Nueva clave IntervaloCalendarioDisponibilidadMinutos:
      intervalo entre horarios de llegada ofrecidos en el
      calendario de disponibilidad (HU-RES-002), antes
      hardcodeado en 30 minutos en el codigo.
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
IF OBJECT_ID('dbo.ReservaMesa', 'U') IS NOT NULL DROP TABLE dbo.ReservaMesa;
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
    PosX       INT           NOT NULL DEFAULT 0, -- posicion en el canvas del editor visual de mesas
    PosY       INT           NOT NULL DEFAULT 0,
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
    CorreoPersonal     NVARCHAR(120) NULL, -- Backup para recuperacion de contrasena (HU-AUT-003)
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
   Al quitarse, la fila no se borra: queda con Activo = 0 y se
   guarda quien y cuando la quito. La accion tambien queda
   registrada en HistorialCierre.
   ------------------------------------------------------------ */
CREATE TABLE dbo.CierreFijo (
    IdCierreFijo         INT IDENTITY(1,1) NOT NULL,
    DiaSemana            TINYINT   NOT NULL, -- 0=Domingo ... 6=Sabado
    FechaCreacion        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioCreacion    INT       NOT NULL,
    Activo               BIT       NOT NULL DEFAULT 1, -- 0 = cierre quitado
    FechaEliminacion     DATETIME2 NULL,
    IdUsuarioEliminacion INT       NULL,
    CONSTRAINT PK_CierreFijo PRIMARY KEY (IdCierreFijo),
    CONSTRAINT CK_CierreFijo_Dia CHECK (DiaSemana BETWEEN 0 AND 6),
    CONSTRAINT FK_CierreFijo_Usuario
        FOREIGN KEY (IdUsuarioCreacion) REFERENCES dbo.Usuario (IdUsuario),
    CONSTRAINT FK_CierreFijo_UsuarioEliminacion
        FOREIGN KEY (IdUsuarioEliminacion) REFERENCES dbo.Usuario (IdUsuario)
);
GO

-- Un solo cierre activo por dia; un dia quitado puede volver a marcarse.
CREATE UNIQUE INDEX UQ_CierreFijo_Dia ON dbo.CierreFijo (DiaSemana) WHERE Activo = 1;
GO

/* ------------------------------------------------------------
   Entidad: CierreEventual (HU-CFG-003)
   Fechas puntuales cerradas (evento, mantenimiento, etc.).
   Las filas se conservan siempre (necesario para el reporte
   de impacto de HU-REP-002, que cuenta cancelaciones en
   cascada por cierre). Al quitar un cierre se marca Activo = 0
   y se guarda quien y cuando lo quito.
   ------------------------------------------------------------ */
CREATE TABLE dbo.CierreEventual (
    IdCierreEventual     INT IDENTITY(1,1) NOT NULL,
    Fecha                DATE          NOT NULL,
    Motivo               NVARCHAR(300) NOT NULL,
    FechaCreacion        DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioCreacion    INT           NOT NULL,
    Activo               BIT           NOT NULL DEFAULT 1, -- 0 = cierre quitado
    FechaEliminacion     DATETIME2     NULL,
    IdUsuarioEliminacion INT           NULL,
    CONSTRAINT PK_CierreEventual PRIMARY KEY (IdCierreEventual),
    CONSTRAINT FK_CierreEventual_Usuario
        FOREIGN KEY (IdUsuarioCreacion) REFERENCES dbo.Usuario (IdUsuario),
    CONSTRAINT FK_CierreEventual_UsuarioEliminacion
        FOREIGN KEY (IdUsuarioEliminacion) REFERENCES dbo.Usuario (IdUsuario)
);
GO

-- Un solo cierre activo por fecha; una fecha quitada puede volver a cerrarse.
CREATE UNIQUE INDEX UQ_CierreEventual_Fecha ON dbo.CierreEventual (Fecha) WHERE Activo = 1;
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
    Etiqueta               NVARCHAR(150) NULL, -- nombre amigable para la pantalla de Parametros; si es NULL se muestra la Clave
    Descripcion            NVARCHAR(300) NULL, -- que almacena y para que se usa, mostrado como texto de ayuda bajo el input
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
CREATE INDEX IX_ReservaMesa_Mesa ON dbo.ReservaMesa (IdMesa);
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
   Cliente centinela para walk-ins (HU-OPE-004): nunca una persona
   real. No eliminar; si se agrega borrado de clientes, excluirlo.
   ------------------------------------------------------------ */
INSERT INTO dbo.Cliente (Nombre, Apellido, Telefono) VALUES
    ('Walk-in', 'Sin reserva', '00000000');
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
   Datos de prueba: mesas, con la misma distribucion (capacidad y
   posicion en el canvas del editor visual) que la base de
   desarrollo. Todas inician 'Disponible'.
   ------------------------------------------------------------ */
INSERT INTO dbo.Mesa (NumeroMesa, Capacidad, Estado, PosX, PosY) VALUES
    (1, 2, 'Disponible', 395, 256),
    (2, 2, 'Disponible', 230, 352),
    (3, 4, 'Disponible', 383,  80),
    (4, 4, 'Disponible', 623, 166),
    (5, 9, 'Disponible', 952, 342);
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
   Datos base: parametros de ConfiguracionSistema.
   IdUsuarioModificacion queda NULL porque estos valores
   iniciales no fueron modificados por ningun administrador.

   - UmbralConfirmacionAutomatica (HU-CFG-001): 4 personas.
   - DuracionReservaMinutos (HU-CFG-006): duracion estimada de
     un turno/reserva, antes hardcodeada en el codigo. Se usa
     para detectar solapamiento entre reservas de una misma
     mesa y para el calculo de disponibilidad del calendario.
   - VentanaAlertaReservaProximaMinutos: ventana de bloqueo
     para asignacion de walk-ins -- no se sienta a un cliente
     sin reserva en una mesa cuya proxima reserva confirmada
     esta a punto de llegar. Independiente de
     DuracionReservaMinutos (deliberadamente mas corta).
   - AnticipacionRecordatorioHoras (HU-CFG-006 / HU-NOT-003):
     cuantas horas antes de la reserva se envia el recordatorio
     automatico al cliente.
   - IntervaloCalendarioDisponibilidadMinutos (HU-RES-002): cada
     cuantos minutos se ofrece un horario de llegada seleccionable
     en el calendario de disponibilidad (antes hardcodeado en 30
     minutos en el codigo).
   ------------------------------------------------------------ */
INSERT INTO dbo.ConfiguracionSistema (Clave, Valor, Etiqueta, Descripcion) VALUES
    ('UmbralConfirmacionAutomatica', '4',
        'Umbral de confirmación automática',
        'Cantidad de personas a partir de la cual una reserva nueva queda Pendiente de revisión en vez de confirmarse sola.'),
    ('DuracionReservaMinutos', '90',
        'Duración de reserva (minutos)',
        'Tiempo estimado que ocupa una reserva en su mesa. Se usa para detectar solapamiento entre reservas y para el cálculo de disponibilidad del calendario.'),
    ('VentanaAlertaReservaProximaMinutos', '30',
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
   Datos base: primera cuenta de Administrador General
   (HU-AUT-006 - bootstrap). Se crea por script porque ningun
   administrador puede existir todavia para crearla desde el
   sistema.
 
   IMPORTANTE: Contransena de prueba es 'Test@123' es un marcador, NO una
   contrasena en texto plano ni un hash valido. Antes de usar
   esta cuenta, la aplicacion debe generar un hash real (ej.
   bcrypt) para una contrasena definida por el equipo y
   reemplazar este valor -- nunca ejecutar este INSERT en un
   ambiente real sin hacerlo primero.
   ------------------------------------------------------------ */
INSERT INTO dbo.Usuario (NombreUsuario, NombreCompleto, CorreoElectronico, ContrasenaHash, Rol) VALUES
    ('admin', 'Administrador General', 'admin@miradormontecarlo.com', '$2a$11$uq75mtIS2h2j4frwtVXWqehailPBy18igZljAHV0FKwDgWNHsmp0W', 'General');
GO