using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexopostal.Reparto.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repartidores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoEmpleado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OficinaJsonId = table.Column<int>(type: "integer", nullable: false),
                    OficinaNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoVehiculo = table.Column<int>(type: "integer", nullable: false),
                    MatriculaVehiculo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repartidores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RutasReparto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaReparto = table.Column<DateOnly>(type: "date", nullable: false),
                    RepartidorId = table.Column<int>(type: "integer", nullable: false),
                    OficinaOrigenJsonId = table.Column<int>(type: "integer", nullable: false),
                    OficinaOrigenNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    HoraSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HoraRegreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutasReparto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutasReparto_Repartidores_RepartidorId",
                        column: x => x.RepartidorId,
                        principalTable: "Repartidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntregasPaquetes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RutaRepartoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NumeroSeguimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DireccionEntrega = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreDestinatario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TelefonoDestinatario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NumeroIntento = table.Column<int>(type: "integer", nullable: false),
                    OrdenEnRuta = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaIntento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceptorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceptorDni = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LatitudEntrega = table.Column<double>(type: "double precision", nullable: true),
                    LongitudEntrega = table.Column<double>(type: "double precision", nullable: true),
                    FirmaDigital = table.Column<string>(type: "text", nullable: true),
                    FotoEntrega = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasPaquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregasPaquetes_RutasReparto_RutaRepartoId",
                        column: x => x.RutaRepartoId,
                        principalTable: "RutasReparto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntregasPaquetes_Estado",
                table: "EntregasPaquetes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasPaquetes_NumeroExpedicion",
                table: "EntregasPaquetes",
                column: "NumeroExpedicion");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasPaquetes_NumeroSeguimiento",
                table: "EntregasPaquetes",
                column: "NumeroSeguimiento");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasPaquetes_Ruta_Orden",
                table: "EntregasPaquetes",
                columns: new[] { "RutaRepartoId", "OrdenEnRuta" });

            migrationBuilder.CreateIndex(
                name: "IX_Repartidores_CodigoEmpleado",
                table: "Repartidores",
                column: "CodigoEmpleado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repartidores_IdentityUserId",
                table: "Repartidores",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repartidores_OficinaJsonId",
                table: "Repartidores",
                column: "OficinaJsonId");

            migrationBuilder.CreateIndex(
                name: "IX_RutasReparto_Codigo",
                table: "RutasReparto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RutasReparto_Estado",
                table: "RutasReparto",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_RutasReparto_FechaReparto",
                table: "RutasReparto",
                column: "FechaReparto");

            migrationBuilder.CreateIndex(
                name: "IX_RutasReparto_Repartidor_Fecha",
                table: "RutasReparto",
                columns: new[] { "RepartidorId", "FechaReparto" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregasPaquetes");

            migrationBuilder.DropTable(
                name: "RutasReparto");

            migrationBuilder.DropTable(
                name: "Repartidores");
        }
    }
}
