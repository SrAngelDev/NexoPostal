using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexopostal.Intranet.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorialEstadosAndOperariosOficina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialEstados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NumeroSeguimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstadoPrevio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TipoUbicacion = table.Column<int>(type: "integer", nullable: false),
                    UbicacionId = table.Column<int>(type: "integer", nullable: true),
                    UbicacionNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UbicacionCodigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OperarioId = table.Column<int>(type: "integer", nullable: true),
                    OperarioNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VisibleParaCliente = table.Column<bool>(type: "boolean", nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstados_OperariosCta_OperarioId",
                        column: x => x.OperarioId,
                        principalTable: "OperariosCta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OperariosOficina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoEmpleado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false),
                    OficinaJsonId = table.Column<int>(type: "integer", nullable: false),
                    OficinaNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperariosOficina", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_Expedicion_Fecha",
                table: "HistorialEstados",
                columns: new[] { "NumeroExpedicion", "FechaEvento" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_FechaEvento",
                table: "HistorialEstados",
                column: "FechaEvento");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_NumeroExpedicion",
                table: "HistorialEstados",
                column: "NumeroExpedicion");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_NumeroSeguimiento",
                table: "HistorialEstados",
                column: "NumeroSeguimiento");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_OperarioId",
                table: "HistorialEstados",
                column: "OperarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OperariosOficina_Identity_Oficina",
                table: "OperariosOficina",
                columns: new[] { "IdentityUserId", "OficinaJsonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperariosOficina_IdentityUserId",
                table: "OperariosOficina",
                column: "IdentityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperariosOficina_OficinaJsonId",
                table: "OperariosOficina",
                column: "OficinaJsonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialEstados");

            migrationBuilder.DropTable(
                name: "OperariosOficina");
        }
    }
}
