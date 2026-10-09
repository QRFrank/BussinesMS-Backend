using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class ProductoAliasEmpaque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reescrita a mano. Une el Ajuste 2 (alias) y el Ajuste 3 (empaque en Producto, sin presentaciones).

            // Ajuste 2: el Nombre actual pasa a Descripcion conservando los valores
            // y se agrega Nombre como alias opcional.
            migrationBuilder.DropIndex(
                name: "UX_Producto_Proveedor_Nombre",
                table: "Producto");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "Producto",
                newName: "Descripcion");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Producto",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Producto_Proveedor_Descripcion",
                table: "Producto",
                columns: new[] { "ProveedorId", "Descripcion" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Producto_Temporada_Nombre",
                table: "Producto",
                columns: new[] { "TemporadaId", "Nombre" },
                unique: true,
                filter: "[Nombre] IS NOT NULL AND [IsActive] = 1");

            // Ajuste 3: empaque opcional en el producto
            migrationBuilder.AddColumn<int>(
                name: "UnidadesPorEmpaque",
                table: "Producto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEmpaque",
                table: "Producto",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Datos: por producto, la presentación activa con Unidades > 1;
            // prioridad EsPrincipal, luego más Unidades, desempate por menor Id.
            migrationBuilder.Sql(@"
UPDATE p
SET p.UnidadesPorEmpaque = x.Unidades,
    p.NombreEmpaque = LEFT(x.Nombre, 20)
FROM Producto p
CROSS APPLY (
    SELECT TOP 1 pp.Unidades, pp.Nombre
    FROM ProductoPresentacion pp
    WHERE pp.ProductoId = p.Id
      AND pp.IsActive = 1
      AND pp.Unidades > 1
    ORDER BY pp.EsPrincipal DESC, pp.Unidades DESC, pp.Id
) x;");

            migrationBuilder.DropTable(
                name: "ProductoPresentacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ajuste 3 (inverso): se recrea ProductoPresentacion con su esquema anterior
            // (20261001233759_Catalogo + rename PrecioVenta -> PrecioUnitario de 20261002010339_CatalogoAjuste1).
            migrationBuilder.CreateTable(
                name: "ProductoPresentacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ProductoPresentacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoPresentacion_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ProductoPresentacion_Producto_Unidades",
                table: "ProductoPresentacion",
                columns: new[] { "ProductoId", "Unidades" },
                unique: true,
                filter: "[IsActive] = 1");

            // No se reinsertan presentaciones desde el empaque: el esquema anterior exigía la presentación
            // de 1 unidad, una principal y un precio por presentación, datos que el empaque no tiene.
            // La tabla queda vacía tras el Down.

            migrationBuilder.DropColumn(
                name: "NombreEmpaque",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "UnidadesPorEmpaque",
                table: "Producto");

            // Ajuste 2 (inverso)
            migrationBuilder.DropIndex(
                name: "UX_Producto_Temporada_Nombre",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "UX_Producto_Proveedor_Descripcion",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Producto");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "Producto",
                newName: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UX_Producto_Proveedor_Nombre",
                table: "Producto",
                columns: new[] { "ProveedorId", "Nombre" },
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
