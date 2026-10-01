using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class QuitarPagoProveedorDeGastoOperativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GastosOperativos_PagosCompra_PagoCompraId",
                table: "GastosOperativos");

            migrationBuilder.DropIndex(
                name: "IX_GastosOperativos_PagoCompraId",
                table: "GastosOperativos");

            migrationBuilder.DropColumn(
                name: "EsPagoProveedor",
                table: "GastosOperativos");

            migrationBuilder.DropColumn(
                name: "PagoCompraId",
                table: "GastosOperativos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsPagoProveedor",
                table: "GastosOperativos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PagoCompraId",
                table: "GastosOperativos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GastosOperativos_PagoCompraId",
                table: "GastosOperativos",
                column: "PagoCompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_GastosOperativos_PagosCompra_PagoCompraId",
                table: "GastosOperativos",
                column: "PagoCompraId",
                principalTable: "PagosCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
