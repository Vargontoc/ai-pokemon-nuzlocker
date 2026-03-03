using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace es.vargontoc.nuzlocke.ai.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNuzlockeRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NuzlockeRegistries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NuzlockeRegistries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NuzlockeId = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NuzlockeRegistries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NuzlockeRegistries_NuzlockeId",
                table: "NuzlockeRegistries",
                column: "NuzlockeId",
                unique: true);
        }
    }
}
