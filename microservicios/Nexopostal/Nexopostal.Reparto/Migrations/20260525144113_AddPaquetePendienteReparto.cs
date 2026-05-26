using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexopostal.Reparto.Migrations
{
    /// <inheritdoc />
    public partial class AddPaquetePendienteReparto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaquetesPendientesReparto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NumeroSeguimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CtaId = table.Column<int>(type: "integer", nullable: false),
                    CtaCodigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreDestinatario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TelefonoDestinatario = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DireccionEntrega = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CodigoPostalDestino = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CiudadDestino = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EsUrgente = table.Column<bool>(type: "boolean", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AsignadoARutaId = table.Column<int>(type: "integer", nullable: true),
                    EntregaPaqueteId = table.Column<int>(type: "integer", nullable: true),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AsignadoPorIdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaquetesPendientesReparto", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesPendientesReparto_Cta_Asignacion",
                table: "PaquetesPendientesReparto",
                columns: new[] { "CtaId", "AsignadoARutaId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesPendientesReparto_NumeroExpedicion",
                table: "PaquetesPendientesReparto",
                column: "NumeroExpedicion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaquetesPendientesReparto");
        }
    }
}
