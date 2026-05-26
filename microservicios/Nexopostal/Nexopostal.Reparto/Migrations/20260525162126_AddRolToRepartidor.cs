using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexopostal.Reparto.Migrations
{
    /// <inheritdoc />
    public partial class AddRolToRepartidor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Repartidores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Repartidor");

            // Backfill: marcar como JefeReparto los repartidores cuyo CodigoEmpleado
            // empiece por "JRP" (convención de los seeds existentes).
            migrationBuilder.Sql(
                "UPDATE \"Repartidores\" SET \"Rol\" = 'JefeReparto' WHERE \"CodigoEmpleado\" LIKE 'JRP%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Repartidores");
        }
    }
}
