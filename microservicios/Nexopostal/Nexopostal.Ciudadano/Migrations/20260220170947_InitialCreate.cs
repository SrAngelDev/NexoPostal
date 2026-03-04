using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexopostal.Ciudadano.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientePerfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DNI = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DireccionPredeterminada = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientePerfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Envios",
                columns: table => new
                {
                    NumeroSeguimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PesoKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Dimensiones = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Origen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Destino = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodigoPostalDestino = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EstadoActual = table.Column<int>(type: "integer", nullable: false),
                    EstadoInternoActual = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CosteCalculado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Pagado = table.Column<bool>(type: "boolean", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NombreRemitente = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApellidosRemitente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TelefonoRemitente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmailRemitente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DniRemitente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NombreDestinatario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApellidosDestinatario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TelefonoDestinatario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmailDestinatario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DniDestinatario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CodigoPostalOrigen = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TipoTarifa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TiempoEntregaEstimado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StripeSessionId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envios", x => x.NumeroSeguimiento);
                });

            migrationBuilder.CreateTable(
                name: "Oficinas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Horario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: true),
                    Longitud = table.Column<double>(type: "double precision", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oficinas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DireccionesFavoritas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientePerfilId = table.Column<int>(type: "integer", nullable: false),
                    Alias = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreDestinatario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DireccionesFavoritas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DireccionesFavoritas_ClientePerfiles_ClientePerfilId",
                        column: x => x.ClientePerfilId,
                        principalTable: "ClientePerfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientePerfil_IdentityUserId",
                table: "ClientePerfiles",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DireccionFavorita_ClientePerfilId",
                table: "DireccionesFavoritas",
                column: "ClientePerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_CodigoPostalDestino",
                table: "Envios",
                column: "CodigoPostalDestino");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_EstadoInternoActual",
                table: "Envios",
                column: "EstadoInternoActual");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_FechaCreacion",
                table: "Envios",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_IdentityUserId",
                table: "Envios",
                column: "IdentityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_NumeroExpedicion",
                table: "Envios",
                column: "NumeroExpedicion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DireccionesFavoritas");

            migrationBuilder.DropTable(
                name: "Envios");

            migrationBuilder.DropTable(
                name: "Oficinas");

            migrationBuilder.DropTable(
                name: "ClientePerfiles");
        }
    }
}
