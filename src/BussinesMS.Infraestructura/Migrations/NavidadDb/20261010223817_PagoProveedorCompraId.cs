using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    // El vinculo compra-pago pasa de Compra.PagoProveedorId (1 a 1) a PagoProveedor.CompraId (1 a muchos).
    // Los vinculos existentes se copian antes de borrar la columna vieja.
    public partial class PagoProveedorCompraId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompraId",
                table: "PagoProveedor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedor_CompraId",
                table: "PagoProveedor",
                column: "CompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_PagoProveedor_Compra_CompraId",
                table: "PagoProveedor",
                column: "CompraId",
                principalTable: "Compra",
                principalColumn: "Id");

            // Conservar los vinculos existentes
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.CompraId = c.Id
                FROM PagoProveedor p
                INNER JOIN Compra c ON c.PagoProveedorId = p.Id;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Compra_PagoProveedor_PagoProveedorId",
                table: "Compra");

            migrationBuilder.DropIndex(
                name: "IX_Compra_PagoProveedorId",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "PagoProveedorId",
                table: "Compra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PagoProveedorId",
                table: "Compra",
                type: "int",
                nullable: true);

            // Vuelve a 1 a 1: queda el primer pago vinculado de cada compra
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.PagoProveedorId = (SELECT MIN(p.Id) FROM PagoProveedor p WHERE p.CompraId = c.Id)
                FROM Compra c;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_PagoProveedorId",
                table: "Compra",
                column: "PagoProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compra_PagoProveedor_PagoProveedorId",
                table: "Compra",
                column: "PagoProveedorId",
                principalTable: "PagoProveedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_PagoProveedor_Compra_CompraId",
                table: "PagoProveedor");

            migrationBuilder.DropIndex(
                name: "IX_PagoProveedor_CompraId",
                table: "PagoProveedor");

            migrationBuilder.DropColumn(
                name: "CompraId",
                table: "PagoProveedor");
        }
    }
}
