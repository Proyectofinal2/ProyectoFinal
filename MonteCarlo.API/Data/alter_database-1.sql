/* ============================================================
   ALTER - Automatizacion de asignacion de mesas (fase 1)
   Ejecutar sobre una base de datos MonteCarlo ya existente.
   Idempotente: puede re-ejecutarse sin error si ya se aplico.

   Contenido:
   1. Reserva-Mesa pasa de FK simple (Reserva.IdMesa) a
      relacion N*N via la nueva tabla ReservaMesa.
   2. Nuevos parametros en ConfiguracionSistema:
      DuracionReservaMinutos, VentanaAlertaReservaProximaMinutos,
      AnticipacionRecordatorioHoras.
   3. ConfiguracionSistema agrega Etiqueta y Descripcion (pantalla
      dinamica de Parametros), con los valores iniciales de las
      4 claves existentes.
   4. Nueva clave IntervaloCalendarioDisponibilidadMinutos
      (reemplaza la constante IntervaloLlegada, hardcodeada en
      30 minutos en ReservasService).
   ============================================================ */

USE MonteCarlo;
GO

/* ------------------------------------------------------------
   1. Nueva tabla ReservaMesa (N*N). Unica fuente de verdad
      para la(s) mesa(s) de una reserva, sea 1 o una
      combinacion de 2 mesas adyacentes. Se crea ANTES de
      tocar Reserva.IdMesa para poder migrar los datos
      existentes hacia ella.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.ReservaMesa', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReservaMesa (
        IdReserva  INT NOT NULL,
        IdMesa     INT NOT NULL,
        CONSTRAINT PK_ReservaMesa PRIMARY KEY (IdReserva, IdMesa),
        CONSTRAINT FK_ReservaMesa_Reserva
            FOREIGN KEY (IdReserva) REFERENCES dbo.Reserva (IdReserva),
        CONSTRAINT FK_ReservaMesa_Mesa
            FOREIGN KEY (IdMesa) REFERENCES dbo.Mesa (IdMesa)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReservaMesa_Mesa' AND object_id = OBJECT_ID('dbo.ReservaMesa'))
    CREATE INDEX IX_ReservaMesa_Mesa ON dbo.ReservaMesa (IdMesa);
GO

/* ------------------------------------------------------------
   2. Migrar las asignaciones existentes (Reserva.IdMesa) a
      ReservaMesa antes de eliminar la columna -- no se pierde
      ninguna mesa ya asignada en reservas actuales.
   ------------------------------------------------------------ */
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reserva') AND name = 'IdMesa')
BEGIN
    INSERT INTO dbo.ReservaMesa (IdReserva, IdMesa)
    SELECT r.IdReserva, r.IdMesa
    FROM dbo.Reserva r
    WHERE r.IdMesa IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM dbo.ReservaMesa rm
          WHERE rm.IdReserva = r.IdReserva AND rm.IdMesa = r.IdMesa
      );
END
GO

/* ------------------------------------------------------------
   3. Quitar el indice, la FK y la columna Reserva.IdMesa --
      ya migrada a ReservaMesa en el paso anterior.
   ------------------------------------------------------------ */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reserva_Mesa' AND object_id = OBJECT_ID('dbo.Reserva'))
    DROP INDEX IX_Reserva_Mesa ON dbo.Reserva;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reserva_Mesa')
    ALTER TABLE dbo.Reserva DROP CONSTRAINT FK_Reserva_Mesa;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reserva') AND name = 'IdMesa')
    ALTER TABLE dbo.Reserva DROP COLUMN IdMesa;
GO

/* ------------------------------------------------------------
   4. Nuevos parametros de configuracion (no se tocan los
      existentes, ej. UmbralConfirmacionAutomatica).
   ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave = 'DuracionReservaMinutos')
    INSERT INTO dbo.ConfiguracionSistema (Clave, Valor) VALUES ('DuracionReservaMinutos', '90');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave = 'VentanaAlertaReservaProximaMinutos')
    INSERT INTO dbo.ConfiguracionSistema (Clave, Valor) VALUES ('VentanaAlertaReservaProximaMinutos', '30');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave = 'AnticipacionRecordatorioHoras')
    INSERT INTO dbo.ConfiguracionSistema (Clave, Valor) VALUES ('AnticipacionRecordatorioHoras', '24');
GO

/* ------------------------------------------------------------
   5. Etiqueta y Descripcion en ConfiguracionSistema: la pantalla
      de Parametros es dinamica (pinta lo que exista en la tabla),
      estas columnas permiten que una clave nueva traiga su propio
      nombre amigable y texto de ayuda sin tocar codigo.
   ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConfiguracionSistema') AND name = 'Etiqueta')
    ALTER TABLE dbo.ConfiguracionSistema ADD Etiqueta NVARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConfiguracionSistema') AND name = 'Descripcion')
    ALTER TABLE dbo.ConfiguracionSistema ADD Descripcion NVARCHAR(300) NULL;
GO

/* Solo rellena Etiqueta/Descripcion si siguen en NULL, para no pisar un
   texto que un admin ya haya personalizado manualmente en la base. */
UPDATE dbo.ConfiguracionSistema
SET Etiqueta = 'Umbral de confirmación automática',
    Descripcion = 'Cantidad de personas a partir de la cual una reserva nueva queda Pendiente de revisión en vez de confirmarse sola.'
WHERE Clave = 'UmbralConfirmacionAutomatica' AND Etiqueta IS NULL;
GO

UPDATE dbo.ConfiguracionSistema
SET Etiqueta = 'Duración de reserva (minutos)',
    Descripcion = 'Tiempo estimado que ocupa una reserva en su mesa. Se usa para detectar solapamiento entre reservas y para el cálculo de disponibilidad del calendario.'
WHERE Clave = 'DuracionReservaMinutos' AND Etiqueta IS NULL;
GO

UPDATE dbo.ConfiguracionSistema
SET Etiqueta = 'Ventana de alerta de reserva próxima (minutos)',
    Descripcion = 'Minutos antes de una reserva confirmada en los que la mesa deja de poder asignarse a un walk-in sin reserva. Independiente de la duración de reserva.'
WHERE Clave = 'VentanaAlertaReservaProximaMinutos' AND Etiqueta IS NULL;
GO

UPDATE dbo.ConfiguracionSistema
SET Etiqueta = 'Anticipación del recordatorio (horas)',
    Descripcion = 'Horas antes de la reserva en que se envía el recordatorio automático al cliente.'
WHERE Clave = 'AnticipacionRecordatorioHoras' AND Etiqueta IS NULL;
GO

/* ------------------------------------------------------------
   6. IntervaloCalendarioDisponibilidadMinutos: reemplaza la
      constante IntervaloLlegada (30 min) hardcodeada en
      ReservasService, usada para generar los horarios de
      llegada seleccionables en el calendario de disponibilidad.
   ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionSistema WHERE Clave = 'IntervaloCalendarioDisponibilidadMinutos')
    INSERT INTO dbo.ConfiguracionSistema (Clave, Valor, Etiqueta, Descripcion) VALUES (
        'IntervaloCalendarioDisponibilidadMinutos', '30',
        'Intervalo del calendario de disponibilidad (minutos)',
        'Cada cuántos minutos se muestra un horario de llegada seleccionable en el calendario de disponibilidad.');
GO
