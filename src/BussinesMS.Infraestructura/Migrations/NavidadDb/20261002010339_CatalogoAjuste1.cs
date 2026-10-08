using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    // Editada a mano para preservar datos existentes (rename en vez de drop/add, FK en dos pasos, categorías semilla)
    public partial class CatalogoAjuste1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Categorías de producto (globales)
            migrationBuilder.CreateTable(
                name: "CategoriaProductoNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUsuarioId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUsuarioId = table.Column<int>(type: "int", nullable: true),
                    DeletedByUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaProductoNav", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaProductoNav_Nombre",
                table: "CategoriaProductoNav",
                column: "Nombre",
                unique: true);

            // 2. Categorías semilla
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM CategoriaProductoNav WHERE Nombre = N'Panetones')
    INSERT INTO CategoriaProductoNav (Nombre, IsActive, CreatedAt, CreatedByUsuarioId) VALUES (N'Panetones', 1, SYSUTCDATETIME(), 1);
IF NOT EXISTS (SELECT 1 FROM CategoriaProductoNav WHERE Nombre = N'Galletas')
    INSERT INTO CategoriaProductoNav (Nombre, IsActive, CreatedAt, CreatedByUsuarioId) VALUES (N'Galletas', 1, SYSUTCDATETIME(), 1);");

            // 3. PrecioVenta -> PrecioUnitario (conserva los datos)
            migrationBuilder.RenameColumn(
                name: "PrecioVenta",
                table: "ProductoPresentacion",
                newName: "PrecioUnitario");

            // 4. PrecioCatalogo inicializado con el precio de compra
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCatalogo",
                table: "Producto",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE Producto SET PrecioCatalogo = PrecioCompraUnidad");

            // 5. CategoriaProductoId: nullable -> asignar Panetones -> NOT NULL -> índice -> FK
            migrationBuilder.AddColumn<int>(
                name: "CategoriaProductoId",
                table: "Producto",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE Producto SET CategoriaProductoId = (SELECT Id FROM CategoriaProductoNav WHERE Nombre = N'Panetones')");

            migrationBuilder.AlterColumn<int>(
                name: "CategoriaProductoId",
                table: "Producto",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CategoriaProductoId",
                table: "Producto",
                column: "CategoriaProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_CategoriaProductoNav_CategoriaProductoId",
                table: "Producto",
                column: "CategoriaProductoId",
                principalTable: "CategoriaProductoNav",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 6. Ya no hay comisión por unidad en el producto
            migrationBuilder.DropColumn(
                name: "ComisionRutaPorUnidad",
                table: "Producto");

            // 7. El sistema no calcula comisiones de vendedores
            migrationBuilder.DropColumn(
                name: "TipoComision",
                table: "Vendedor");

            migrationBuilder.DropColumn(
                name: "PorcentajeComision",
                table: "Vendedor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoComision",
                table: "Vendedor",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeComision",
                table: "Vendedor",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionRutaPorUnidad",
                table: "Producto",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_CategoriaProductoNav_CategoriaProductoId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CategoriaProductoId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CategoriaProductoId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "PrecioCatalogo",
                table: "Producto");

            migrationBuilder.RenameColumn(
                name: "PrecioUnitario",
                table: "ProductoPresentacion",
                newName: "PrecioVenta");

            migrationBuilder.DropTable(
                name: "CategoriaProductoNav");
        }
    }
}
