# Entity Framework Migrations

Este documento describe el proceso de migraciones de base de datos utilizando Entity Framework Core.

## Estructura de Migraciones

Las migraciones se encuentran en `src/McpServer/Migrations/` y gestionan el versionado del esquema de la base de datos SQLite.

### Archivos Generados

Para cada migración se generan 3 archivos:

1. **YYYYMMDDHHMMSS_NombreMigracion.cs** - Código C# con métodos `Up()` y `Down()` para aplicar/revertir cambios
2. **YYYYMMDDHHMMSS_NombreMigracion.Designer.cs** - Metadata de la migración
3. **PokeDbContextModelSnapshot.cs** - Snapshot del modelo actual (se actualiza con cada migración)

Adicionalmente se genera:

4. **migration.sql** - Script SQL idempotente para despliegue en producción

## Comandos Principales

### Crear Nueva Migración

```bash
cd src/McpServer
dotnet ef migrations add NombreDeLaMigracion
```

### Generar Script SQL

```bash
cd src/McpServer
dotnet ef migrations script --output Migrations/migration.sql
```

Para generar script desde una migración específica hasta otra:

```bash
dotnet ef migrations script MigracionOrigen MigracionDestino --output script.sql
```

### Aplicar Migraciones

Las migraciones se aplican automáticamente al iniciar la aplicación gracias a esta configuración en `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokeDbContext>();
    db.Database.Migrate();
}
```

### Revertir Migración

```bash
cd src/McpServer
dotnet ef migrations remove
```

**ADVERTENCIA**: Solo se puede revertir la última migración y únicamente si no ha sido aplicada a la base de datos.

### Listar Migraciones

```bash
cd src/McpServer
dotnet ef migrations list
```

### Ver Estado de Migraciones Aplicadas

Para ver qué migraciones han sido aplicadas en la base de datos:

```sql
SELECT * FROM __EFMigrationsHistory;
```

## Flujo de Trabajo

### Desarrollo Local

1. Modificar entidades en `Data/` (ej: `CachedPokemon.cs`)
2. Crear migración: `dotnet ef migrations add DescripcionDelCambio`
3. Revisar archivos generados en `Migrations/`
4. Ejecutar aplicación (migraciones se aplican automáticamente)
5. Validar con tests: `dotnet test`

### Despliegue a Producción

1. Generar script SQL: `dotnet ef migrations script --output Migrations/migration.sql`
2. Revisar `migration.sql` - es idempotente y puede ejecutarse múltiples veces
3. Desplegar aplicación
4. El script `migration.sql` está disponible para ejecución manual si se requiere
5. Al iniciar, la aplicación ejecuta `db.Database.Migrate()` automáticamente

## Migración Inicial

La migración `20260208125828_InitialCreate` define el esquema base:

### Tablas Creadas

- **CachedPokemons** - Cache de datos de Pokemon
- **CachedMoves** - Cache de movimientos
- **CachedTypes** - Cache de tipos
- **CachedAbilities** - Cache de habilidades

### Estructura Común

Todas las tablas tienen la misma estructura:

```sql
CREATE TABLE "TableName" (
    "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
    "NameOrId" TEXT NOT NULL,
    "JsonData" TEXT NOT NULL,
    "CachedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX "IX_TableName_NameOrId" ON "TableName" ("NameOrId");
```

## Buenas Prácticas

1. **Nombres Descriptivos**: Usar nombres claros que describan el cambio (ej: `AddPokemonEvolutionTable`)
2. **Una Responsabilidad**: Cada migración debe tener un único propósito
3. **Revisar SQL**: Siempre revisar el script SQL generado antes de desplegar
4. **No Modificar Migraciones Aplicadas**: Una vez aplicada en producción, nunca modificar una migración existente
5. **Backups**: Hacer backup de la base de datos antes de aplicar migraciones en producción
6. **Testing**: Validar migraciones en ambiente de desarrollo/staging antes de producción

## Troubleshooting

### Error: "SQLite Error 1: table already exists"

La base de datos fue creada con `EnsureCreated()` en lugar de migraciones. Solución:

```bash
# Borrar base de datos local
rm pokecache.db pokecache.db-shm pokecache.db-wal

# Ejecutar aplicación (se crean tablas con Migrate())
dotnet run
```

### Error: "No migrations configuration type was found"

Asegurarse de tener el paquete `Microsoft.EntityFrameworkCore.Design`:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.11
```

### Ver Contenido de la Base de Datos

```bash
sqlite3 pokecache.db
.tables
.schema CachedPokemons
SELECT * FROM __EFMigrationsHistory;
.quit
```

## Referencias

- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core SQLite Database Provider](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
