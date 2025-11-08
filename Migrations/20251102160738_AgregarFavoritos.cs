using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DondeComemos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFavoritos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RestauranteId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favoritos_Restaurantes_RestauranteId",
                        column: x => x.RestauranteId,
                        principalTable: "Restaurantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_RestauranteId",
                table: "Favoritos",
                column: "RestauranteId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_UserId_RestauranteId",
                table: "Favoritos",
                columns: new[] { "UserId", "RestauranteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favoritos");
        }
    }
}
