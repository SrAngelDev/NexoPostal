using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexopostal.Auth.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renombrar roles en la columna Rol de AspNetUsers (almacenada como string)
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'OperarioCTA'  WHERE ""Rol"" = 'OperarioLogistico';");
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'Supervisor'   WHERE ""Rol"" = 'OperarioJefe';");
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'JefeReparto'  WHERE ""Rol"" = 'RepartidorJefe';");
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'Repartidor'   WHERE ""Rol"" = 'RepartidorLogistico';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'OperarioLogistico' WHERE ""Rol"" = 'OperarioCTA';");
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'OperarioJefe'      WHERE ""Rol"" = 'Supervisor';");
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'RepartidorJefe'    WHERE ""Rol"" = 'JefeReparto';");
            migrationBuilder.Sql(@"UPDATE ""AspNetUsers"" SET ""Rol"" = 'RepartidorLogistico' WHERE ""Rol"" = 'Repartidor' AND ""CodigoEmpleado"" = 'RPL001';");
        }
    }
}
