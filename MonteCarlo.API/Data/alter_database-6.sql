USE MonteCarlo;
GO

/* ============================================================
   alter_database-6.sql - Tolerancia para cobrar (HU-CON-002, cuentas por cobrar)

   Script idempotente para una base YA CREADA con database.sql
   (o con los alter anteriores). Se puede ejecutar varias veces
   y termina siempre en el mismo esquema y datos base que database.sql.

   Agrega la clave ToleranciaCobroMinutos a ConfiguracionSistema:
   minutos de tolerancia despues de terminar el tiempo de uso de la
   mesa (DuracionReservaMinutos) antes de marcar una cuenta abierta
   como antigua, es decir "Por cobrar". La pantalla de Parametros la
   muestra sola (lee todo lo que exista en la tabla).

   Requisito: la tabla dbo.ConfiguracionSistema (alter_database-1.sql).
   Si no existe, el script aborta con un mensaje claro.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.ConfiguracionSistema', 'U') IS NULL
BEGIN
    RAISERROR('Falta dbo.ConfiguracionSistema: ejecuta primero alter_database-1.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave = 'ToleranciaCobroMinutos')
BEGIN
    INSERT INTO dbo.ConfiguracionSistema (Clave, Valor, Etiqueta, Descripcion) VALUES
        ('ToleranciaCobroMinutos', '30',
            N'Tolerancia para cobrar (minutos)',
            N'Minutos que se esperan después de terminar el tiempo de uso de la mesa (duración de reserva) antes de marcar la cuenta abierta como «Por cobrar». Con 0 se marca justo al terminar el turno. Ej.: con turno de 90 min y tolerancia de 30, se marca a las 2 h de haberse sentado el cliente.');
END
GO

SET NOEXEC OFF;
GO
