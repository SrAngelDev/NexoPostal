using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nexopostal.Ciudadano.Migrations
{
    /// <inheritdoc />
    public partial class AddTarifaBandas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TarifasBandas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    OrdenBanda = table.Column<int>(type: "integer", nullable: false),
                    PesoHastaKg = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PrecioBase = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoPorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifasBandas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TarifasBandas",
                columns: new[] { "Id", "FechaModificacion", "ModificadoPorUserId", "OrdenBanda", "PesoHastaKg", "PrecioBase", "Serie" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 1m, 4.50m, 0 },
                    { 2, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 1m, 6.50m, 1 },
                    { 3, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 1m, 5.95m, 2 },
                    { 4, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 1m, 8.95m, 3 },
                    { 5, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2m, 5.25m, 0 },
                    { 6, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2m, 7.75m, 1 },
                    { 7, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2m, 6.95m, 2 },
                    { 8, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2m, 10.50m, 3 },
                    { 9, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 5m, 6.95m, 0 },
                    { 10, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 5m, 10.50m, 1 },
                    { 11, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 5m, 8.95m, 2 },
                    { 12, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 5m, 13.95m, 3 },
                    { 13, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 10m, 9.95m, 0 },
                    { 14, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 10m, 14.95m, 1 },
                    { 15, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 10m, 12.95m, 2 },
                    { 16, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 10m, 19.95m, 3 },
                    { 17, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, 20m, 14.95m, 0 },
                    { 18, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, 20m, 21.95m, 1 },
                    { 19, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, 20m, 18.95m, 2 },
                    { 20, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, 20m, 28.95m, 3 },
                    { 21, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, 30m, 19.95m, 0 },
                    { 22, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, 30m, 29.95m, 1 },
                    { 23, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, 30m, 25.95m, 2 },
                    { 24, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, 30m, 38.95m, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TarifaBanda_Serie_Orden",
                table: "TarifasBandas",
                columns: new[] { "Serie", "OrdenBanda" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TarifasBandas");
        }
    }
}
