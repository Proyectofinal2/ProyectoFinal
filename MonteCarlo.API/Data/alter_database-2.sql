USE MonteCarlo;
GO

/* ------------------------------------------------------------
   1. CierreFijo
   ------------------------------------------------------------ */
IF COL_LENGTH('dbo.CierreFijo', 'Activo') IS NULL
    ALTER TABLE dbo.CierreFijo ADD
        Activo               BIT       NOT NULL CONSTRAINT DF_CierreFijo_Activo DEFAULT 1,
        FechaEliminacion     DATETIME2 NULL,
        IdUsuarioEliminacion INT       NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CierreFijo_UsuarioEliminacion')
    ALTER TABLE dbo.CierreFijo ADD CONSTRAINT FK_CierreFijo_UsuarioEliminacion
        FOREIGN KEY (IdUsuarioEliminacion) REFERENCES dbo.Usuario (IdUsuario);
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_CierreFijo_Dia' AND parent_object_id = OBJECT_ID('dbo.CierreFijo'))
    ALTER TABLE dbo.CierreFijo DROP CONSTRAINT UQ_CierreFijo_Dia;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_CierreFijo_Dia' AND object_id = OBJECT_ID('dbo.CierreFijo'))
    CREATE UNIQUE INDEX UQ_CierreFijo_Dia ON dbo.CierreFijo (DiaSemana) WHERE Activo = 1;
GO

/* ------------------------------------------------------------
   2. CierreEventual
   ------------------------------------------------------------ */
IF COL_LENGTH('dbo.CierreEventual', 'Activo') IS NULL
    ALTER TABLE dbo.CierreEventual ADD
        Activo               BIT       NOT NULL CONSTRAINT DF_CierreEventual_Activo DEFAULT 1,
        FechaEliminacion     DATETIME2 NULL,
        IdUsuarioEliminacion INT       NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CierreEventual_UsuarioEliminacion')
    ALTER TABLE dbo.CierreEventual ADD CONSTRAINT FK_CierreEventual_UsuarioEliminacion
        FOREIGN KEY (IdUsuarioEliminacion) REFERENCES dbo.Usuario (IdUsuario);
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_CierreEventual_Fecha' AND parent_object_id = OBJECT_ID('dbo.CierreEventual'))
    ALTER TABLE dbo.CierreEventual DROP CONSTRAINT UQ_CierreEventual_Fecha;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_CierreEventual_Fecha' AND object_id = OBJECT_ID('dbo.CierreEventual'))
    CREATE UNIQUE INDEX UQ_CierreEventual_Fecha ON dbo.CierreEventual (Fecha) WHERE Activo = 1;
GO
