using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class PagosVendedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagoVendedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ModoComision = table.Column<int>(type: "int", nullable: true),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MontoBase = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Medio = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Anulado = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PagoVendedor", x => x.Id);
                    table.CheckConstraint("CK_PagoVendedor_Monto", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_PagoVendedor_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoVendedor_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagoVendedorDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PagoVendedorId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
                    ComisionUnidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_PagoVendedorDetalle", x => x.Id);
                    table.CheckConstraint("CK_PagoVendedorDetalle_Montos", "[Unidades] > 0 AND [ComisionUnidad] >= 0 AND [Subtotal] >= 0");
                    table.ForeignKey(
                        name: "FK_PagoVendedorDetalle_PagoVendedor_PagoVendedorId",
                        column: x => x.PagoVendedorId,
                        principalTable: "PagoVendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagoVendedorDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoVendedor_TemporadaId_VendedorId_Tipo",
                table: "PagoVendedor",
                columns: new[] { "TemporadaId", "VendedorId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_PagoVendedor_VendedorId",
                table: "PagoVendedor",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "UX_PagoVendedor_ComisionVigente",
                table: "PagoVendedor",
                columns: new[] { "TemporadaId", "VendedorId" },
                unique: true,
                filter: "[Tipo] = 2 AND [Anulado] = 0 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PagoVendedorDetalle_PagoVendedorId_ProductoId",
                table: "PagoVendedorDetalle",
                columns: new[] { "PagoVendedorId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagoVendedorDetalle_ProductoId",
                table: "PagoVendedorDetalle",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoVendedorDetalle");

            migrationBuilder.DropTable(
                name: "PagoVendedor");
        }
    }
}
