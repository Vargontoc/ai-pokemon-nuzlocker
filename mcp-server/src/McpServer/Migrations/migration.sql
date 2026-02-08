CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "CachedAbilities" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CachedAbilities" PRIMARY KEY AUTOINCREMENT,
    "NameOrId" TEXT NOT NULL,
    "JsonData" TEXT NOT NULL,
    "CachedAt" TEXT NOT NULL
);

CREATE TABLE "CachedMoves" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CachedMoves" PRIMARY KEY AUTOINCREMENT,
    "NameOrId" TEXT NOT NULL,
    "JsonData" TEXT NOT NULL,
    "CachedAt" TEXT NOT NULL
);

CREATE TABLE "CachedPokemons" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CachedPokemons" PRIMARY KEY AUTOINCREMENT,
    "NameOrId" TEXT NOT NULL,
    "JsonData" TEXT NOT NULL,
    "CachedAt" TEXT NOT NULL
);

CREATE TABLE "CachedTypes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CachedTypes" PRIMARY KEY AUTOINCREMENT,
    "NameOrId" TEXT NOT NULL,
    "JsonData" TEXT NOT NULL,
    "CachedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX "IX_CachedAbilities_NameOrId" ON "CachedAbilities" ("NameOrId");

CREATE UNIQUE INDEX "IX_CachedMoves_NameOrId" ON "CachedMoves" ("NameOrId");

CREATE UNIQUE INDEX "IX_CachedPokemons_NameOrId" ON "CachedPokemons" ("NameOrId");

CREATE UNIQUE INDEX "IX_CachedTypes_NameOrId" ON "CachedTypes" ("NameOrId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260208125828_InitialCreate', '8.0.11');

COMMIT;

