USE MonteCarlo;
GO

/* ============================================================
   alter_database-9.sql - Historial de métodos de pago

   Script incremental e idempotente para una base ya creada.
   Crea exclusivamente dbo.HistorialMetodoPago y sus índices.
   No modifica registros históricos ni tablas existentes.

   Requisitos: dbo.MetodoPago y dbo.Usuario deben existir.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    IF OBJECT_ID('dbo.MetodoPago', 'U') IS NULL OR OBJECT_ID('dbo.Usuario', 'U') IS NULL
    BEGIN
        ;THROW 50000, N'Faltan las tablas dbo.MetodoPago o dbo.Usuario: ejecuta primero database.sql. No se aplico ningun cambio.', 1;
    END

    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.HistorialMetodoPago', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HistorialMetodoPago (
            IdHistorialMetodoPago INT IDENTITY(1,1) NOT NULL,
            IdOperacion           UNIQUEIDENTIFIER NOT NULL,
            IdMetodoPago          INT              NOT NULL,
            Accion                NVARCHAR(20)     NOT NULL, -- 'Creado' / 'Modificado'
            NombreAnterior        NVARCHAR(30)     NULL,
            NombreNuevo           NVARCHAR(30)     NOT NULL,
            ActivoAnterior        BIT              NULL,
            ActivoNuevo           BIT              NOT NULL,
            IdUsuario             INT              NOT NULL, -- administrador responsable
            Fecha                 DATETIME2        NOT NULL CONSTRAINT DF_HistorialMetodoPago_Fecha DEFAULT SYSDATETIME(),
            CONSTRAINT PK_HistorialMetodoPago PRIMARY KEY (IdHistorialMetodoPago),
            CONSTRAINT CK_HistorialMetodoPago_Accion CHECK (Accion IN ('Creado', 'Modificado')),
            CONSTRAINT CK_HistorialMetodoPago_ValoresAnteriores CHECK (
                (Accion = 'Creado' AND NombreAnterior IS NULL AND ActivoAnterior IS NULL) OR
                (Accion = 'Modificado' AND NombreAnterior IS NOT NULL AND ActivoAnterior IS NOT NULL)
            ),
            CONSTRAINT CK_HistorialMetodoPago_CambioEfectivo CHECK (
                Accion = 'Creado' OR NombreAnterior <> NombreNuevo OR ActivoAnterior <> ActivoNuevo
            ),
            CONSTRAINT FK_HistorialMetodoPago_Metodo
                FOREIGN KEY (IdMetodoPago) REFERENCES dbo.MetodoPago (IdMetodoPago),
            CONSTRAINT FK_HistorialMetodoPago_Usuario
                FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
        );
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialMetodoPago_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialMetodoPago'))
        CREATE INDEX IX_HistorialMetodoPago_Fecha
            ON dbo.HistorialMetodoPago (Fecha DESC, IdHistorialMetodoPago DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialMetodoPago_Metodo_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialMetodoPago'))
        CREATE INDEX IX_HistorialMetodoPago_Metodo_Fecha
            ON dbo.HistorialMetodoPago (IdMetodoPago, Fecha DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialMetodoPago_Usuario_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialMetodoPago'))
        CREATE INDEX IX_HistorialMetodoPago_Usuario_Fecha
            ON dbo.HistorialMetodoPago (IdUsuario, Fecha DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialMetodoPago_Accion_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialMetodoPago'))
        CREATE INDEX IX_HistorialMetodoPago_Accion_Fecha
            ON dbo.HistorialMetodoPago (Accion, Fecha DESC);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
