using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace es.vargontoc.nuzlocke.ai.Migrations
{
    /// <inheritdoc />
    public partial class AddNuzlockeMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NuzlockeMetadatas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Generation = table.Column<int>(type: "INTEGER", nullable: false),
                    LockeType = table.Column<string>(type: "TEXT", nullable: false),
                    IsInitialized = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NuzlockeMetadatas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NuzlockeMetadatas");
        }
    }
}
