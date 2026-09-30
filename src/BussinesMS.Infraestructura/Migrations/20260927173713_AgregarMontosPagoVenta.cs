using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMontosPagoVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cambio",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEfectivo",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibido",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTransferencia",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Backfill de ventas existentes según MetodoPago (1 = Efectivo, 2 = TransferenciaQR)
            migrationBuilder.Sql("UPDATE Ventas SET MontoEfectivo = TotalNeto, MontoRecibido = TotalNeto, Cambio = 0 WHERE MetodoPago = 1");
            migrationBuilder.Sql("UPDATE Ventas SET MontoTransferencia = TotalNeto WHERE MetodoPago = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cambio",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoEfectivo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoRecibido",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoTransferencia",
                table: "Ventas");
        }
    }
}
