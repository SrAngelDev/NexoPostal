using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexopostal.Intranet.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CentrosTratamiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Area = table.Column<int>(type: "integer", nullable: false),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    EsNodoAereo = table.Column<bool>(type: "boolean", nullable: false),
                    EsNodoMaritimo = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosTratamiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosPaquetes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CtaOrigenId = table.Column<int>(type: "integer", nullable: false),
                    CtaDestinoId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    TipoTransporte = table.Column<int>(type: "integer", nullable: false),
                    EsUrgente = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaLlegada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosPaquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosPaquetes_CentrosTratamiento_CtaDestinoId",
                        column: x => x.CtaDestinoId,
                        principalTable: "CentrosTratamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosPaquetes_CentrosTratamiento_CtaOrigenId",
                        column: x => x.CtaOrigenId,
                        principalTable: "CentrosTratamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperariosCta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoEmpleado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false),
                    CentroTratamientoId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperariosCta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperariosCta_CentrosTratamiento_CentroTratamientoId",
                        column: x => x.CentroTratamientoId,
                        principalTable: "CentrosTratamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RutasCta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PrefijoCp = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CtaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutasCta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutasCta_CentrosTratamiento_CtaId",
                        column: x => x.CtaId,
                        principalTable: "CentrosTratamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesPaquetes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OperarioAsignadoId = table.Column<int>(type: "integer", nullable: false),
                    AsignadoPorId = table.Column<int>(type: "integer", nullable: false),
                    CtaId = table.Column<int>(type: "integer", nullable: false),
                    TipoTarea = table.Column<int>(type: "integer", nullable: false),
                    EstadoTarea = table.Column<int>(type: "integer", nullable: false),
                    EsUrgente = table.Column<bool>(type: "boolean", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCompletada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesPaquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesPaquetes_CentrosTratamiento_CtaId",
                        column: x => x.CtaId,
                        principalTable: "CentrosTratamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesPaquetes_OperariosCta_AsignadoPorId",
                        column: x => x.AsignadoPorId,
                        principalTable: "OperariosCta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesPaquetes_OperariosCta_OperarioAsignadoId",
                        column: x => x.OperarioAsignadoId,
                        principalTable: "OperariosCta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incidencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroExpedicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CtaId = table.Column<int>(type: "integer", nullable: false),
                    ReportadaPorId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Resolucion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidencias_CentrosTratamiento_CtaId",
                        column: x => x.CtaId,
                        principalTable: "CentrosTratamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidencias_OperariosCta_ReportadaPorId",
                        column: x => x.ReportadaPorId,
                        principalTable: "OperariosCta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_AsignadoPorId",
                table: "AsignacionesPaquetes",
                column: "AsignadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_Cta_Estado",
                table: "AsignacionesPaquetes",
                columns: new[] { "CtaId", "EstadoTarea" });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_EstadoTarea",
                table: "AsignacionesPaquetes",
                column: "EstadoTarea");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_NumeroExpedicion",
                table: "AsignacionesPaquetes",
                column: "NumeroExpedicion");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPaquetes_Operario_Estado",
                table: "AsignacionesPaquetes",
                columns: new[] { "OperarioAsignadoId", "EstadoTarea" });

            migrationBuilder.CreateIndex(
                name: "IX_CentrosTratamiento_Area",
                table: "CentrosTratamiento",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosTratamiento_Codigo",
                table: "CentrosTratamiento",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidencias_Cta_Estado",
                table: "Incidencias",
                columns: new[] { "CtaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidencias_Estado",
                table: "Incidencias",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Incidencias_NumeroExpedicion",
                table: "Incidencias",
                column: "NumeroExpedicion");

            migrationBuilder.CreateIndex(
                name: "IX_Incidencias_ReportadaPorId",
                table: "Incidencias",
                column: "ReportadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosPaquetes_CtaDestino_Estado",
                table: "MovimientosPaquetes",
                columns: new[] { "CtaDestinoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosPaquetes_CtaOrigen_Estado",
                table: "MovimientosPaquetes",
                columns: new[] { "CtaOrigenId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosPaquetes_Estado",
                table: "MovimientosPaquetes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosPaquetes_NumeroExpedicion",
                table: "MovimientosPaquetes",
                column: "NumeroExpedicion");

            migrationBuilder.CreateIndex(
                name: "IX_OperariosCta_CentroTratamientoId",
                table: "OperariosCta",
                column: "CentroTratamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_OperariosCta_CodigoEmpleado",
                table: "OperariosCta",
                column: "CodigoEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_OperariosCta_IdentityUserId",
                table: "OperariosCta",
                column: "IdentityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperariosCta_Identity_Cta",
                table: "OperariosCta",
                columns: new[] { "IdentityUserId", "CentroTratamientoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RutasCta_CtaId",
                table: "RutasCta",
                column: "CtaId");

            migrationBuilder.CreateIndex(
                name: "IX_RutasCta_PrefijoCp",
                table: "RutasCta",
                column: "PrefijoCp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesPaquetes");

            migrationBuilder.DropTable(
                name: "Incidencias");

            migrationBuilder.DropTable(
                name: "MovimientosPaquetes");

            migrationBuilder.DropTable(
                name: "RutasCta");

            migrationBuilder.DropTable(
                name: "OperariosCta");

            migrationBuilder.DropTable(
                name: "CentrosTratamiento");
        }
    }
}
