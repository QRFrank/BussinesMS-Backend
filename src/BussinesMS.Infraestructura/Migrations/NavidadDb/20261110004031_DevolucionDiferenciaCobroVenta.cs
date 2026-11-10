using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class DevolucionDiferenciaCobroVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CobroVenta_MontoPositivo",
                table: "CobroVenta");

            migrationBuilder.AddColumn<bool>(
                name: "EsDevolucion",
                table: "CobroVenta",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CobroVenta_MontoPositivo",
                table: "CobroVenta",
                sql: "[Monto] > 0 OR ([EsDevolucion] = 1 AND [Monto] < 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CobroVenta_MontoPositivo",
                table: "CobroVenta");

            migrationBuilder.DropColumn(
                name: "EsDevolucion",
                table: "CobroVenta");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CobroVenta_MontoPositivo",
                table: "CobroVenta",
                sql: "[Monto] > 0");
        }
    }
}
