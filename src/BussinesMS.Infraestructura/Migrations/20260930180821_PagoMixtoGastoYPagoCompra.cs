using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class PagoMixtoGastoYPagoCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoCaja",
                table: "PagosCompra",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoExterno",
                table: "PagosCompra",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoCaja",
                table: "GastosOperativos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoExterno",
                table: "GastosOperativos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Relleno de datos existentes: con sesión de caja → todo caja; sin sesión → todo externo
            migrationBuilder.Sql(
                "UPDATE GastosOperativos SET MontoCaja = Monto, MontoExterno = 0 WHERE SesionCajaId IS NOT NULL; " +
                "UPDATE GastosOperativos SET MontoExterno = Monto, MontoCaja = 0 WHERE SesionCajaId IS NULL;");

            migrationBuilder.Sql(
                "UPDATE PagosCompra SET MontoCaja = Monto, MontoExterno = 0 WHERE SesionCajaId IS NOT NULL; " +
                "UPDATE PagosCompra SET MontoExterno = Monto, MontoCaja = 0 WHERE SesionCajaId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoCaja",
                table: "PagosCompra");

            migrationBuilder.DropColumn(
                name: "MontoExterno",
                table: "PagosCompra");

            migrationBuilder.DropColumn(
                name: "MontoCaja",
                table: "GastosOperativos");

            migrationBuilder.DropColumn(
                name: "MontoExterno",
                table: "GastosOperativos");
        }
    }
}
