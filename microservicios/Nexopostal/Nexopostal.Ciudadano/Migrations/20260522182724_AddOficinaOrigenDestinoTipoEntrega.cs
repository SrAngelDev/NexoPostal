using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexopostal.Ciudadano.Migrations
{
    /// <inheritdoc />
    public partial class AddOficinaOrigenDestinoTipoEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OficinaDestinoId",
                table: "Envios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OficinaOrigenId",
                table: "Envios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoEntrega",
                table: "Envios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OficinaDestinoId",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OficinaOrigenId",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "TipoEntrega",
                table: "Envios");
        }
    }
}
