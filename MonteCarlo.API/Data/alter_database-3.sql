USE MonteCarlo;
GO

/* ============================================================
   alter_database-3.sql - Catalogo de productos (HU-CON-001)

   Script unico e idempotente para una base YA CREADA con
   database.sql (o con los alter anteriores). Se puede ejecutar
   varias veces y desde cualquier estado previo:
     - base sin cambios del catalogo,
     - base que ya ejecuto una version anterior de este script
       (cuando se dividia en alter_database-3, -4 y -5).
   Termina siempre en el mismo esquema que database.sql.

   Requisitos: tablas dbo.Usuario y dbo.Producto (database.sql).
   Si faltan, el script se detiene sin cambiar nada.

   Cambios:
   1. Nueva tabla CategoriaProducto: categorias del menu que
      cualquier administrador crea, renombra, reordena y
      deshabilita; solo se elimina una sin productos.
   2. Producto: la columna de texto libre Categoria se reemplaza
      por IdCategoria (FK a CategoriaProducto). Las categorias
      que ya existian como texto se migran automaticamente; los
      productos sin categoria pasan a "Sin categoría". Ademas,
      Nombre pasa a ser unico.
   3. Nueva tabla HistorialCatalogo: auditoria de cambios de
      productos y categorias (quien, cuando, que cambio).
      Una sola tabla para poder paginar y filtrar el historial
      general sin UNION. Admite eliminar categorias sin productos
      y productos sin ventas: al borrarlos, IdProducto o
      IdCategoria pasan a NULL (ON DELETE SET NULL), el nombre se
      conserva en NombreElemento y se registra la accion
      'Eliminado'.
   4. DATOS DE PRUEBA OPCIONALES (seccion 6): categorias y menu
      costarricense. Por defecto NO se cargan.

   Como cargar los datos de prueba (opcional):
      cambia el valor de @value a 1 en la linea de abajo y
      ejecuta el script completo.

   Nota: son cambios de esquema en varios lotes (GO) sin una
   transaccion unica. Si un lote falla, corrige la causa y vuelve
   a ejecutar el script: retoma desde donde quedo.
   ============================================================ */

EXEC sp_set_session_context @key = N'CargarDatosPrueba', @value = 0;  -- 0 = no cargar datos de prueba, 1 = cargarlos
GO

/* ------------------------------------------------------------
   0. Requisitos. Si faltan tablas base, se omite el resto del
      script (SET NOEXEC) en vez de fallar a medias.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.Usuario', 'U') IS NULL OR OBJECT_ID('dbo.Producto', 'U') IS NULL
BEGIN
    RAISERROR(N'Faltan las tablas dbo.Usuario o dbo.Producto: ejecuta primero database.sql. No se aplico ningun cambio.', 16, 1);
    SET NOEXEC ON;
END
GO

/* ------------------------------------------------------------
   1. CategoriaProducto
   El color que se ve en la interfaz se deriva de IdCategoria
   (no se guarda). Orden define el orden del menu y del selector
   de consumo; no es unico para poder intercambiar posiciones.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.CategoriaProducto', 'U') IS NULL
    CREATE TABLE dbo.CategoriaProducto (
        IdCategoria   INT IDENTITY(1,1) NOT NULL,
        Nombre        NVARCHAR(50) NOT NULL,
        Orden         INT          NOT NULL,
        Activa        BIT          NOT NULL CONSTRAINT DF_CategoriaProducto_Activa DEFAULT 1,
        FechaCreacion DATETIME2    NOT NULL CONSTRAINT DF_CategoriaProducto_Fecha DEFAULT SYSDATETIME(),
        CONSTRAINT PK_CategoriaProducto PRIMARY KEY (IdCategoria),
        CONSTRAINT UQ_CategoriaProducto_Nombre UNIQUE (Nombre),
        CONSTRAINT CK_CategoriaProducto_Orden CHECK (Orden > 0)
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CategoriaProducto_Orden' AND object_id = OBJECT_ID('dbo.CategoriaProducto'))
    CREATE INDEX IX_CategoriaProducto_Orden ON dbo.CategoriaProducto (Orden);
GO

/* ------------------------------------------------------------
   2. Producto: Categoria (texto) -> IdCategoria (FK)
   ------------------------------------------------------------ */
IF COL_LENGTH('dbo.Producto', 'IdCategoria') IS NULL
    ALTER TABLE dbo.Producto ADD IdCategoria INT NULL;
GO

-- 2.1 Migrar las categorias de texto libre. Va en SQL dinamico porque
--     la columna Categoria deja de existir al final de esta seccion y
--     un re-ejecutado fallaria al compilar el lote.
IF COL_LENGTH('dbo.Producto', 'Categoria') IS NOT NULL
BEGIN
    EXEC(N'
    INSERT INTO dbo.CategoriaProducto (Nombre, Orden)
    SELECT d.Nombre, ISNULL((SELECT MAX(Orden) FROM dbo.CategoriaProducto), 0) + ROW_NUMBER() OVER (ORDER BY d.Nombre)
    FROM (SELECT DISTINCT LTRIM(RTRIM(Categoria)) AS Nombre
          FROM dbo.Producto
          WHERE LTRIM(RTRIM(ISNULL(Categoria, N''''))) <> N'''') d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto c WHERE c.Nombre = d.Nombre);

    IF EXISTS (SELECT 1 FROM dbo.Producto WHERE LTRIM(RTRIM(ISNULL(Categoria, N''''))) = N'''')
       AND NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Nombre = N''Sin categoría'')
        INSERT INTO dbo.CategoriaProducto (Nombre, Orden)
        VALUES (N''Sin categoría'', ISNULL((SELECT MAX(Orden) FROM dbo.CategoriaProducto), 0) + 1);

    UPDATE p SET IdCategoria = c.IdCategoria
    FROM dbo.Producto p
    JOIN dbo.CategoriaProducto c
        ON c.Nombre = CASE WHEN LTRIM(RTRIM(ISNULL(p.Categoria, N''''))) = N'''' THEN N''Sin categoría''
                           ELSE LTRIM(RTRIM(p.Categoria)) END
    WHERE p.IdCategoria IS NULL;
    ');
END
GO

-- 2.2 Endurecer la columna nueva y quitar la vieja
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Producto') AND name = 'IdCategoria' AND is_nullable = 1)
    ALTER TABLE dbo.Producto ALTER COLUMN IdCategoria INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Producto_Categoria' AND parent_object_id = OBJECT_ID('dbo.Producto'))
    ALTER TABLE dbo.Producto ADD CONSTRAINT FK_Producto_Categoria
        FOREIGN KEY (IdCategoria) REFERENCES dbo.CategoriaProducto (IdCategoria);
GO

IF COL_LENGTH('dbo.Producto', 'Categoria') IS NOT NULL
    ALTER TABLE dbo.Producto DROP COLUMN Categoria;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Producto_Categoria' AND object_id = OBJECT_ID('dbo.Producto'))
    CREATE INDEX IX_Producto_Categoria ON dbo.Producto (IdCategoria);
GO

-- 2.3 Nombre unico. Si ya hay productos con el mismo nombre no se crea la restriccion:
--     corrige los duplicados (renombra o deshabilita) y vuelve a ejecutar el script.
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Producto_Nombre' AND parent_object_id = OBJECT_ID('dbo.Producto'))
BEGIN
    IF EXISTS (SELECT Nombre FROM dbo.Producto GROUP BY Nombre HAVING COUNT(*) > 1)
        PRINT 'ADVERTENCIA: hay productos con nombre duplicado; no se creo UQ_Producto_Nombre. Corrige los duplicados y vuelve a ejecutar.';
    ELSE
        ALTER TABLE dbo.Producto ADD CONSTRAINT UQ_Producto_Nombre UNIQUE (Nombre);
END
GO

/* ------------------------------------------------------------
   3. HistorialCatalogo (estado final)
   Auditoria de productos y categorias. Una fila por campo cambiado.
   - Producto: Creado, Editado (Campo = Nombre / Precio / Categoria),
     Habilitado, Deshabilitado, Eliminado.
   - Categoria: Creado, Editado (Campo = Nombre), Reordenado
     (Campo = Posicion), Habilitado, Deshabilitado, Eliminado.
   Precio se guarda como numero en colones sin formato (la interfaz
   le da formato). Motivo explica los cambios en cascada, p. ej.
   'Por deshabilitar la categoría «Bebidas»'.
   IdProducto / IdCategoria quedan NULL cuando el elemento se
   elimino; entonces NombreElemento conserva su nombre.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.HistorialCatalogo', 'U') IS NULL
    CREATE TABLE dbo.HistorialCatalogo (
        IdHistorialCatalogo INT IDENTITY(1,1) NOT NULL,
        TipoElemento        NVARCHAR(10)  NOT NULL, -- 'Producto' / 'Categoria'
        IdProducto          INT           NULL,     -- NULL si el producto se elimino
        IdCategoria         INT           NULL,     -- NULL si la categoria se elimino
        NombreElemento      NVARCHAR(100) NULL,     -- nombre del producto o categoria eliminados
        Accion              NVARCHAR(20)  NOT NULL,
        Campo               NVARCHAR(30)  NULL,
        ValorAnterior       NVARCHAR(200) NULL,
        ValorNuevo          NVARCHAR(200) NULL,
        Motivo              NVARCHAR(300) NULL,
        IdUsuario           INT           NOT NULL, -- administrador responsable
        Fecha               DATETIME2     NOT NULL CONSTRAINT DF_HistorialCatalogo_Fecha DEFAULT SYSDATETIME(),
        CONSTRAINT PK_HistorialCatalogo PRIMARY KEY (IdHistorialCatalogo),
        CONSTRAINT CK_HistorialCatalogo_Tipo CHECK (TipoElemento IN ('Producto', 'Categoria')),
        CONSTRAINT CK_HistorialCatalogo_Accion CHECK (Accion IN ('Creado', 'Editado', 'Reordenado', 'Habilitado', 'Deshabilitado', 'Eliminado')),
        CONSTRAINT CK_HistorialCatalogo_Elemento CHECK (
            (TipoElemento = 'Producto'  AND (IdProducto  IS NOT NULL OR NombreElemento IS NOT NULL) AND IdCategoria IS NULL) OR
            (TipoElemento = 'Categoria' AND (IdCategoria IS NOT NULL OR NombreElemento IS NOT NULL) AND IdProducto  IS NULL)
        ),
        CONSTRAINT CK_HistorialCatalogo_Campo CHECK (
            (Accion IN ('Editado', 'Reordenado') AND Campo IS NOT NULL) OR
            (Accion NOT IN ('Editado', 'Reordenado'))
        ),
        CONSTRAINT FK_HistorialCatalogo_Producto
            FOREIGN KEY (IdProducto) REFERENCES dbo.Producto (IdProducto) ON DELETE SET NULL,
        CONSTRAINT FK_HistorialCatalogo_Categoria
            FOREIGN KEY (IdCategoria) REFERENCES dbo.CategoriaProducto (IdCategoria) ON DELETE SET NULL,
        CONSTRAINT FK_HistorialCatalogo_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
    );
GO

/* ------------------------------------------------------------
   4. HistorialCatalogo: llevar una tabla ya existente al estado final
      (no hace nada si la tabla se acaba de crear arriba). Cada paso
      es un lote aparte para que SQL Server resuelva las columnas
      nuevas en el siguiente.
   ------------------------------------------------------------ */

-- 4.1 El CHECK de elemento se recrea al final; antes hay que quitarlo si
--     esta desactualizado (el final nombra NombreElemento dos veces).
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_HistorialCatalogo_Elemento' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo')
             AND (LEN(definition) - LEN(REPLACE(definition, 'NombreElemento', ''))) / LEN('NombreElemento') < 2)
    ALTER TABLE dbo.HistorialCatalogo DROP CONSTRAINT CK_HistorialCatalogo_Elemento;
GO

-- 4.2 NombreElemento: crear o ampliar a 100 caracteres (200 bytes)
IF COL_LENGTH('dbo.HistorialCatalogo', 'NombreElemento') IS NULL
    ALTER TABLE dbo.HistorialCatalogo ADD NombreElemento NVARCHAR(100) NULL;
ELSE IF COL_LENGTH('dbo.HistorialCatalogo', 'NombreElemento') < 200
    ALTER TABLE dbo.HistorialCatalogo ALTER COLUMN NombreElemento NVARCHAR(100) NULL;
GO

-- 4.3 FK de producto y de categoria con ON DELETE SET NULL
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_HistorialCatalogo_Producto' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo')
             AND delete_referential_action <> 2)
    ALTER TABLE dbo.HistorialCatalogo DROP CONSTRAINT FK_HistorialCatalogo_Producto;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HistorialCatalogo_Producto' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    ALTER TABLE dbo.HistorialCatalogo ADD CONSTRAINT FK_HistorialCatalogo_Producto
        FOREIGN KEY (IdProducto) REFERENCES dbo.Producto (IdProducto) ON DELETE SET NULL;

IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_HistorialCatalogo_Categoria' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo')
             AND delete_referential_action <> 2)
    ALTER TABLE dbo.HistorialCatalogo DROP CONSTRAINT FK_HistorialCatalogo_Categoria;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HistorialCatalogo_Categoria' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    ALTER TABLE dbo.HistorialCatalogo ADD CONSTRAINT FK_HistorialCatalogo_Categoria
        FOREIGN KEY (IdCategoria) REFERENCES dbo.CategoriaProducto (IdCategoria) ON DELETE SET NULL;
GO

-- 4.4 Accion 'Eliminado'
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_HistorialCatalogo_Accion' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo')
             AND definition NOT LIKE N'%Eliminado%')
    ALTER TABLE dbo.HistorialCatalogo DROP CONSTRAINT CK_HistorialCatalogo_Accion;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HistorialCatalogo_Accion' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    ALTER TABLE dbo.HistorialCatalogo ADD CONSTRAINT CK_HistorialCatalogo_Accion
        CHECK (Accion IN ('Creado', 'Editado', 'Reordenado', 'Habilitado', 'Deshabilitado', 'Eliminado'));
GO

-- 4.5 CHECK de elemento: un producto o categoria eliminados dejan su Id NULL pero conservan el nombre
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HistorialCatalogo_Elemento' AND parent_object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    ALTER TABLE dbo.HistorialCatalogo ADD CONSTRAINT CK_HistorialCatalogo_Elemento CHECK (
        (TipoElemento = 'Producto'  AND (IdProducto  IS NOT NULL OR NombreElemento IS NOT NULL) AND IdCategoria IS NULL) OR
        (TipoElemento = 'Categoria' AND (IdCategoria IS NOT NULL OR NombreElemento IS NOT NULL) AND IdProducto  IS NULL)
    );
GO

/* ------------------------------------------------------------
   5. Indices de apoyo (historial paginado y filtrado)
   ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialCatalogo_Fecha' AND object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    CREATE INDEX IX_HistorialCatalogo_Fecha ON dbo.HistorialCatalogo (Fecha DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialCatalogo_Tipo' AND object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    CREATE INDEX IX_HistorialCatalogo_Tipo ON dbo.HistorialCatalogo (TipoElemento, Fecha DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialCatalogo_Usuario' AND object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    CREATE INDEX IX_HistorialCatalogo_Usuario ON dbo.HistorialCatalogo (IdUsuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialCatalogo_Producto' AND object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    CREATE INDEX IX_HistorialCatalogo_Producto ON dbo.HistorialCatalogo (IdProducto, Fecha DESC) WHERE IdProducto IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialCatalogo_Categoria' AND object_id = OBJECT_ID('dbo.HistorialCatalogo'))
    CREATE INDEX IX_HistorialCatalogo_Categoria ON dbo.HistorialCatalogo (IdCategoria, Fecha DESC) WHERE IdCategoria IS NOT NULL;
GO

/* ------------------------------------------------------------
   6. DATOS DE PRUEBA (OPCIONAL)
   Solo se cargan si arriba pusiste @value = 1. Es seguro
   re-ejecutarlo: no duplica categorias ni productos (compara por
   nombre). No crea usuarios ni historial.
   NO ejecutar con datos de prueba en produccion.
   ------------------------------------------------------------ */
IF CONVERT(INT, ISNULL(SESSION_CONTEXT(N'CargarDatosPrueba'), 0)) = 1
BEGIN
    SET NOCOUNT ON;

    -- 6.1 Categorias (el orden de la lista es la columna Orden)
    INSERT INTO dbo.CategoriaProducto (Nombre, Orden)
    SELECT v.Nombre, ISNULL((SELECT MAX(Orden) FROM dbo.CategoriaProducto), 0) + ROW_NUMBER() OVER (ORDER BY v.N)
    FROM (VALUES
        (1, N'Entradas'),
        (2, N'Platos fuertes'),
        (3, N'Postres'),
        (4, N'Bebidas'),
        (5, N'Cervezas y licores')
    ) AS v(N, Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto c WHERE c.Nombre = v.Nombre);

    -- 6.2 Productos (menu costarricense, precios en colones). Disponible = 0 en los marcados como deshabilitados.
    INSERT INTO dbo.Producto (Nombre, IdCategoria, Precio, Disponible)
    SELECT v.Nombre, c.IdCategoria, v.Precio, v.Disponible
    FROM (VALUES
        (1, N'Ceviche de pescado', N'Entradas', 4500, 1),
        (2, N'Patacones con frijoles molidos', N'Entradas', 3200, 1),
        (3, N'Sopa de mondongo', N'Entradas', 3800, 0),
        (4, N'Casado con pollo', N'Platos fuertes', 5500, 1),
        (5, N'Casado con bistec encebollado', N'Platos fuertes', 6500, 1),
        (6, N'Arroz con pollo', N'Platos fuertes', 4800, 1),
        (7, N'Tres leches', N'Postres', 2800, 1),
        (8, N'Flan de coco', N'Postres', 2600, 0),
        (9, N'Fresco natural de mora', N'Bebidas', 1800, 1),
        (10, N'Café chorreado', N'Bebidas', 1500, 1),
        (11, N'Copa de vino tinto', N'Cervezas y licores', 4200, 1),
        (12, N'Cerveza Imperial', N'Cervezas y licores', 1800, 1),
        (13, N'Cerveza Pilsen', N'Cervezas y licores', 1800, 1),
        (14, N'Cerveza Bavaria Gold', N'Cervezas y licores', 2000, 1),
        (15, N'Trago de guaro Cacique', N'Cervezas y licores', 1500, 0),
        (16, N'Chifrijo', N'Entradas', 4800, 1),
        (17, N'Yuca frita con chicharrón', N'Entradas', 3800, 1),
        (18, N'Empanada de queso', N'Entradas', 1500, 1),
        (19, N'Sopa negra', N'Entradas', 3200, 1),
        (20, N'Gallo pinto con huevo', N'Platos fuertes', 3200, 1),
        (21, N'Olla de carne', N'Platos fuertes', 5800, 1),
        (22, N'Casado con pescado', N'Platos fuertes', 6200, 1),
        (23, N'Arroz con camarones', N'Platos fuertes', 8500, 1),
        (24, N'Chuleta de cerdo', N'Platos fuertes', 5800, 1),
        (25, N'Picadillo de papa', N'Platos fuertes', 3500, 1),
        (26, N'Arroz con leche', N'Postres', 2200, 1),
        (27, N'Cajeta de coco', N'Postres', 1200, 1),
        (28, N'Queque seco', N'Postres', 1800, 1),
        (29, N'Capuchino', N'Bebidas', 2400, 1),
        (30, N'Shot de espresso', N'Bebidas', 1000, 1),
        (31, N'Agua de pipa', N'Bebidas', 2500, 1),
        (32, N'Shot de guaro Cacique', N'Cervezas y licores', 1500, 1),
        (33, N'Shot de tequila', N'Cervezas y licores', 2500, 1),
        (34, N'Shot de ron Centenario', N'Cervezas y licores', 2500, 1),
        (35, N'Shot de whisky', N'Cervezas y licores', 3000, 1),
        (36, N'Shot de vodka', N'Cervezas y licores', 2500, 1),
        (37, N'Cerveza Rock Ice', N'Cervezas y licores', 2000, 1)
    ) AS v(N, Nombre, Categoria, Precio, Disponible)
    JOIN dbo.CategoriaProducto c ON c.Nombre = v.Categoria
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Producto p WHERE p.Nombre = v.Nombre)
    ORDER BY v.N;

    PRINT 'Datos de prueba del catalogo cargados.';
END
ELSE
    PRINT 'Datos de prueba omitidos (CargarDatosPrueba = 0).';
GO

-- Reactiva la ejecucion si la seccion 0 la habia detenido (no afecta si no se detuvo)
SET NOEXEC OFF;
GO
