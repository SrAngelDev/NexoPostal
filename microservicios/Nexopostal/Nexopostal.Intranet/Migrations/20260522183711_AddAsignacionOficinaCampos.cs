using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexopostal.Intranet.Migrations
{
    /// <inheritdoc />
    public partial class AddAsignacionOficinaCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OperarioAsignadoId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CtaId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "AsignadoPorId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "OficinaJsonId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OficinaNombre",
                table: "AsignacionesPaquetes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperarioOficinaAsignadoId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_Oficina_Estado",
                table: "AsignacionesPaquetes",
                columns: new[] { "OficinaJsonId", "EstadoTarea" });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_OperarioOficina_Estado",
                table: "AsignacionesPaquetes",
                columns: new[] { "OperarioOficinaAsignadoId", "EstadoTarea" });

            migrationBuilder.AddForeignKey(
                name: "FK_AsignacionesPaquetes_OperariosOficina_OperarioOficinaAsigna~",
                table: "AsignacionesPaquetes",
                column: "OperarioOficinaAsignadoId",
                principalTable: "OperariosOficina",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsignacionesPaquetes_OperariosOficina_OperarioOficinaAsigna~",
                table: "AsignacionesPaquetes");

            migrationBuilder.DropIndex(
                name: "IX_AsignacionesPaquetes_Oficina_Estado",
                table: "AsignacionesPaquetes");

            migrationBuilder.DropIndex(
                name: "IX_AsignacionesPaquetes_OperarioOficina_Estado",
                table: "AsignacionesPaquetes");

            migrationBuilder.DropColumn(
                name: "OficinaJsonId",
                table: "AsignacionesPaquetes");

            migrationBuilder.DropColumn(
                name: "OficinaNombre",
                table: "AsignacionesPaquetes");

            migrationBuilder.DropColumn(
                name: "OperarioOficinaAsignadoId",
                table: "AsignacionesPaquetes");

            migrationBuilder.AlterColumn<int>(
                name: "OperarioAsignadoId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CtaId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AsignadoPorId",
                table: "AsignacionesPaquetes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
