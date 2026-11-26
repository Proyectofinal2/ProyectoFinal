USE MonteCarlo;
GO

/* IVA: incremental, no destructivo e idempotente. Las ventas existentes conservan NULL. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    IF OBJECT_ID('dbo.Venta', 'U') IS NULL OR OBJECT_ID('dbo.ConfiguracionSistema', 'U') IS NULL
        THROW 50000, N'Faltan las tablas Venta o ConfiguracionSistema. No se aplicaron cambios.', 1;

    BEGIN TRANSACTION;
    IF COL_LENGTH('dbo.Venta', 'Subtotal') IS NULL
        ALTER TABLE dbo.Venta ADD Subtotal DECIMAL(10,2) NULL;
    IF COL_LENGTH('dbo.Venta', 'TasaIvaAplicada') IS NULL
        ALTER TABLE dbo.Venta ADD TasaIvaAplicada DECIMAL(5,2) NULL;
    IF COL_LENGTH('dbo.Venta', 'ImporteIva') IS NULL
        ALTER TABLE dbo.Venta ADD ImporteIva DECIMAL(10,2) NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave = 'TasaIva')
        INSERT INTO dbo.ConfiguracionSistema (Clave, Valor, Etiqueta, Descripcion)
        VALUES ('TasaIva', '13', N'IVA (%)', N'Porcentaje de IVA aplicado a las ventas nuevas. Entre 0 y 100, con hasta dos decimales.');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
