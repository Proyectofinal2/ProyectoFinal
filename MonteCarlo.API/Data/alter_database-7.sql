USE MonteCarlo;
GO

/* ============================================================
   alter_database-7.sql - Estado Abandonada para cuentas cerradas sin cobro (HU-CON-002, Fase B)

   Script idempotente para una base YA CREADA con database.sql
   (o con los alter anteriores). Se puede ejecutar varias veces
   y termina siempre en el mismo esquema que database.sql.

   Una cuenta abierta con productos que el cliente se fue sin pagar
   no puede cobrarse (seria un ingreso falso) ni liberarse sin consumo.
   Se cierra como 'Abandonada': conserva el total y las lineas (queda
   registrado cuanto se consumio sin cobrar), no tiene cobro ni metodo
   de pago, y guarda quien, cuando y por que la cerro reutilizando
   MotivoAnulacion / FechaAnulacion / IdUsuarioAnulacion. No es ingreso:
   los reportes siguen filtrando Estado = 'Cobrada'.

   Cambios en dbo.Venta:
   - CK_Venta_Estado: agrega 'Abandonada'.
   - CK_Venta_Cobro: Abierta y Abandonada no tienen cobro.
   - CK_Venta_Anulacion: motivo, fecha y usuario obligatorios para
     Anulada y Abandonada; vacios en los demas estados.
   - CK_Venta_Abandonada (nueva): solo consumo en mesa, sin metodo de
     pago ni datos de cobro.

   Requisito: alter_database-5.sql (ventas y cuenta de mesa).
   Si falta, el script aborta con un mensaje claro.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Venta', 'U') IS NULL OR COL_LENGTH('dbo.Venta', 'FechaCobro') IS NULL
BEGIN
    RAISERROR('Falta el esquema de ventas y cuenta de mesa: ejecuta primero alter_database-5.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

/* Las restricciones se recrean solo si todavia no conocen el estado Abandonada. */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Estado' AND parent_object_id = OBJECT_ID('dbo.Venta') AND definition NOT LIKE '%Abandonada%')
    ALTER TABLE dbo.Venta DROP CONSTRAINT CK_Venta_Estado;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Cobro' AND parent_object_id = OBJECT_ID('dbo.Venta') AND definition NOT LIKE '%Abandonada%')
    ALTER TABLE dbo.Venta DROP CONSTRAINT CK_Venta_Cobro;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Anulacion' AND parent_object_id = OBJECT_ID('dbo.Venta') AND definition NOT LIKE '%Abandonada%')
    ALTER TABLE dbo.Venta DROP CONSTRAINT CK_Venta_Anulacion;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Estado' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Estado
        CHECK (Estado IN ('Abierta', 'Cobrada', 'Anulada', 'Abandonada'));

-- Una cuenta abandonada es de consumo en mesa y nunca se cobro: sin metodo de pago ni datos de cobro.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Abandonada' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Abandonada CHECK (
        Estado <> 'Abandonada' OR
        (TipoVenta = 'Consumo' AND IdMetodoPago IS NULL AND FechaCobro IS NULL AND IdUsuarioCobro IS NULL)
    );

-- Cobrada (y Anulada, que fue cobrada antes) exige fecha y usuario de cobro y, si hay total, metodo de pago.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Cobro' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Cobro CHECK (
        Estado IN ('Abierta', 'Abandonada') OR
        (FechaCobro IS NOT NULL AND IdUsuarioCobro IS NOT NULL AND (Total = 0 OR IdMetodoPago IS NOT NULL))
    );

-- Anulada y Abandonada exigen motivo, fecha y usuario de la baja; en los demas estados los tres quedan vacios.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_Anulacion' AND parent_object_id = OBJECT_ID('dbo.Venta'))
    ALTER TABLE dbo.Venta ADD CONSTRAINT CK_Venta_Anulacion CHECK (
        (Estado IN ('Anulada', 'Abandonada') AND MotivoAnulacion IS NOT NULL AND FechaAnulacion IS NOT NULL AND IdUsuarioAnulacion IS NOT NULL) OR
        (Estado NOT IN ('Anulada', 'Abandonada') AND MotivoAnulacion IS NULL AND FechaAnulacion IS NULL AND IdUsuarioAnulacion IS NULL)
    );
GO

SET NOEXEC OFF;
GO
