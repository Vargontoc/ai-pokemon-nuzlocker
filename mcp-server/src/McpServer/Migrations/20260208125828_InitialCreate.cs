using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace es.vargontoc.nuzlocke.ai.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedAbilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NameOrId = table.Column<string>(type: "TEXT", nullable: false),
                    JsonData = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedAbilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedMoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NameOrId = table.Column<string>(type: "TEXT", nullable: false),
                    JsonData = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedMoves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedPokemons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NameOrId = table.Column<string>(type: "TEXT", nullable: false),
                    JsonData = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedPokemons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NameOrId = table.Column<string>(type: "TEXT", nullable: false),
                    JsonData = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NameOrId = table.Column<string>(type: "TEXT", nullable: false),
                    JsonData = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedAbilities_NameOrId",
                table: "CachedAbilities",
                column: "NameOrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedMoves_NameOrId",
                table: "CachedMoves",
                column: "NameOrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedPokemons_NameOrId",
                table: "CachedPokemons",
                column: "NameOrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedTypes_NameOrId",
                table: "CachedTypes",
                column: "NameOrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedItems_NameOrId",
                table: "CachedItems",
                column: "NameOrId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedAbilities");

            migrationBuilder.DropTable(
                name: "CachedMoves");

            migrationBuilder.DropTable(
                name: "CachedPokemons");

            migrationBuilder.DropTable(
                name: "CachedTypes");

            migrationBuilder.DropTable(
                name: "CachedItems");
        }
    }
}
