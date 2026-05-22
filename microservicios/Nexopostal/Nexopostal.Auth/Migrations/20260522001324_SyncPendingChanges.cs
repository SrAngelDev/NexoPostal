using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexopostal.Auth.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EliminadoEnUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EliminadoPorId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EliminadoEnUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EliminadoPorId",
                table: "AspNetUsers");
        }
    }
}
