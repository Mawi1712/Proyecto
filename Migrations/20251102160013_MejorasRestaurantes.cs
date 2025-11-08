using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DondeComemos.Migrations
{
    /// <inheritdoc />
    public partial class MejorasRestaurantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TipoCocina",
                table: "Restaurantes",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RangoPrecios",
                table: "Restaurantes",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AccesibleDiscapacitados",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceptaReservas",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ambiente",
                table: "Restaurantes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryDisponible",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleMapsUrl",
                table: "Restaurantes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OpcionesVeganas",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OpcionesVegetarianas",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneEstacionamiento",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WifiGratis",
                table: "Restaurantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccesibleDiscapacitados",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "AceptaReservas",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "Ambiente",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "DeliveryDisponible",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "GoogleMapsUrl",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "OpcionesVeganas",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "OpcionesVegetarianas",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "TieneEstacionamiento",
                table: "Restaurantes");

            migrationBuilder.DropColumn(
                name: "WifiGratis",
                table: "Restaurantes");

            migrationBuilder.AlterColumn<string>(
                name: "TipoCocina",
                table: "Restaurantes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "RangoPrecios",
                table: "Restaurantes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
