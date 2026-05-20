using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexopostal.Reparto.Migrations
{
    /// <inheritdoc />
    public partial class AddUbicacionRepartidor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UbicacionesRepartidores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepartidorId = table.Column<int>(type: "integer", nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: false),
                    Longitud = table.Column<double>(type: "double precision", nullable: false),
                    RutaActivaId = table.Column<int>(type: "integer", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbicacionesRepartidores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UbicacionesRepartidores_Repartidores_RepartidorId",
                        column: x => x.RepartidorId,
                        principalTable: "Repartidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesRepartidores_RepartidorId",
                table: "UbicacionesRepartidores",
                column: "RepartidorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UbicacionesRepartidores");
        }
    }
}
