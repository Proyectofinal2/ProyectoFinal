USE MonteCarlo;
GO

/* ============================================================
   alter_database-8.sql - Editar cuentas de administrador y su historial (HU-AUT-007)

   Script idempotente para una base YA CREADA con database.sql
   (o con los alter anteriores). Se puede ejecutar varias veces
   y termina siempre en el mismo esquema que database.sql.

   Crea dbo.HistorialUsuario: auditoria de cuentas de administrador
   (quien creo, edito, deshabilito o habilito una cuenta). Una fila
   por campo cambiado en una edicion y una fila por alta,
   deshabilitacion o habilitacion. Nunca guarda contrasenas.
   Las dos FK a dbo.Usuario no tienen cascada (las cuentas no se
   eliminan, y SQL Server no permite dos rutas de cascada).

   Requisito: dbo.Usuario (database.sql). No requiere otros alter.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Usuario', 'U') IS NULL
BEGIN
    RAISERROR('Falta la tabla dbo.Usuario: crea la base con database.sql primero.', 16, 1);
    SET NOEXEC ON;
END
GO

IF OBJECT_ID('dbo.HistorialUsuario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialUsuario (
        IdHistorialUsuario    INT IDENTITY(1,1) NOT NULL,
        IdUsuarioAfectado     INT           NOT NULL,
        NombreUsuarioAfectado NVARCHAR(120) NOT NULL, -- nombre completo al momento del cambio
        Accion                NVARCHAR(20)  NOT NULL,
        Campo                 NVARCHAR(30)  NULL,
        ValorAnterior         NVARCHAR(200) NULL,
        ValorNuevo            NVARCHAR(200) NULL,
        Motivo                NVARCHAR(300) NULL,
        IdUsuario             INT           NOT NULL, -- administrador responsable
        Fecha                 DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_HistorialUsuario PRIMARY KEY (IdHistorialUsuario),
        CONSTRAINT CK_HistorialUsuario_Accion CHECK (Accion IN ('Creada', 'Editada', 'Deshabilitada', 'Habilitada')),
        CONSTRAINT CK_HistorialUsuario_Campo CHECK (
            (Accion = 'Editada' AND Campo IS NOT NULL) OR (Accion <> 'Editada')
        ),
        CONSTRAINT FK_HistorialUsuario_Afectado
            FOREIGN KEY (IdUsuarioAfectado) REFERENCES dbo.Usuario (IdUsuario),
        CONSTRAINT FK_HistorialUsuario_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialUsuario_Fecha' AND object_id = OBJECT_ID('dbo.HistorialUsuario'))
    CREATE INDEX IX_HistorialUsuario_Fecha ON dbo.HistorialUsuario (Fecha DESC, IdHistorialUsuario DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialUsuario_Afectado' AND object_id = OBJECT_ID('dbo.HistorialUsuario'))
    CREATE INDEX IX_HistorialUsuario_Afectado ON dbo.HistorialUsuario (IdUsuarioAfectado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialUsuario_Usuario' AND object_id = OBJECT_ID('dbo.HistorialUsuario'))
    CREATE INDEX IX_HistorialUsuario_Usuario ON dbo.HistorialUsuario (IdUsuario);
GO

SET NOEXEC OFF;
GO
