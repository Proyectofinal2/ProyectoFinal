USE MonteCarlo;
GO

/* ============================================================
   alter_database-4.sql - Historial de horarios de operacion
   (HU-CFG-005)

   Script idempotente para una base MonteCarlo ya creada.
   Crea exclusivamente dbo.HistorialHorarioOperacion y sus
   indices de consulta. No modifica registros ni otras tablas.

   Requisitos: dbo.HorarioOperacion y dbo.Usuario deben existir.
   Si falta alguna, el script se detiene sin aplicar cambios.
   ============================================================ */

BEGIN TRY
    /* ------------------------------------------------------------
       0. Requisitos
       ------------------------------------------------------------ */
    IF OBJECT_ID('dbo.HorarioOperacion', 'U') IS NULL OR OBJECT_ID('dbo.Usuario', 'U') IS NULL
    BEGIN
        ;THROW 50000, N'Faltan las tablas dbo.HorarioOperacion o dbo.Usuario: ejecuta primero database.sql. No se aplico ningun cambio.', 1;
    END

    BEGIN TRANSACTION;

    /* ------------------------------------------------------------
       1. HistorialHorarioOperacion
       Una fila representa el cambio efectivo de un dia. Las filas
       de un mismo guardado en lote comparten IdOperacion.
       ------------------------------------------------------------ */
    IF OBJECT_ID('dbo.HistorialHorarioOperacion', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HistorialHorarioOperacion (
        IdHistorialHorarioOperacion INT IDENTITY(1,1) NOT NULL,
        IdOperacion                 UNIQUEIDENTIFIER NOT NULL,
        IdHorarioOperacion          INT              NOT NULL,
        DiaSemana                   TINYINT          NOT NULL, -- 0=Domingo ... 6=Sabado
        Accion                      NVARCHAR(20)     NOT NULL, -- 'Creado' / 'Modificado'
        HoraAperturaAnterior        TIME             NULL,
        HoraAperturaNueva           TIME             NOT NULL,
        HoraCierreAnterior          TIME             NULL,
        HoraCierreNuevo             TIME             NOT NULL,
        ActivoAnterior              BIT              NULL,
        ActivoNuevo                 BIT              NOT NULL,
        IdUsuario                   INT              NOT NULL, -- administrador responsable
        Fecha                       DATETIME2        NOT NULL CONSTRAINT DF_HistorialHorarioOperacion_Fecha DEFAULT SYSDATETIME(),
        CONSTRAINT PK_HistorialHorarioOperacion PRIMARY KEY (IdHistorialHorarioOperacion),
        CONSTRAINT CK_HistorialHorarioOperacion_Dia CHECK (DiaSemana BETWEEN 0 AND 6),
        CONSTRAINT CK_HistorialHorarioOperacion_Accion CHECK (Accion IN ('Creado', 'Modificado')),
        CONSTRAINT CK_HistorialHorarioOperacion_RangoNuevo CHECK (HoraCierreNuevo > HoraAperturaNueva),
        CONSTRAINT CK_HistorialHorarioOperacion_ValoresAnteriores CHECK (
            (Accion = 'Creado' AND HoraAperturaAnterior IS NULL AND HoraCierreAnterior IS NULL AND ActivoAnterior IS NULL) OR
            (Accion = 'Modificado' AND HoraAperturaAnterior IS NOT NULL AND HoraCierreAnterior IS NOT NULL AND
             ActivoAnterior IS NOT NULL AND HoraCierreAnterior > HoraAperturaAnterior)
        ),
        CONSTRAINT CK_HistorialHorarioOperacion_CambioEfectivo CHECK (
            Accion = 'Creado' OR HoraAperturaAnterior <> HoraAperturaNueva OR
            HoraCierreAnterior <> HoraCierreNuevo OR ActivoAnterior <> ActivoNuevo
        ),
        CONSTRAINT FK_HistorialHorarioOperacion_Horario
            FOREIGN KEY (IdHorarioOperacion) REFERENCES dbo.HorarioOperacion (IdHorarioOperacion),
        CONSTRAINT FK_HistorialHorarioOperacion_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
        );
    END

    /* ------------------------------------------------------------
       2. Indices de apoyo para filtros, orden y agrupacion por lote
       ------------------------------------------------------------ */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialHorarioOperacion_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialHorarioOperacion'))
        CREATE INDEX IX_HistorialHorarioOperacion_Fecha
            ON dbo.HistorialHorarioOperacion (Fecha DESC, IdHistorialHorarioOperacion DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialHorarioOperacion_Dia_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialHorarioOperacion'))
        CREATE INDEX IX_HistorialHorarioOperacion_Dia_Fecha
            ON dbo.HistorialHorarioOperacion (DiaSemana, Fecha DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialHorarioOperacion_Usuario_Fecha'
                   AND object_id = OBJECT_ID('dbo.HistorialHorarioOperacion'))
        CREATE INDEX IX_HistorialHorarioOperacion_Usuario_Fecha
            ON dbo.HistorialHorarioOperacion (IdUsuario, Fecha DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialHorarioOperacion_Operacion'
                   AND object_id = OBJECT_ID('dbo.HistorialHorarioOperacion'))
        CREATE INDEX IX_HistorialHorarioOperacion_Operacion
            ON dbo.HistorialHorarioOperacion (IdOperacion, Fecha DESC);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    THROW;
END CATCH;
GO
