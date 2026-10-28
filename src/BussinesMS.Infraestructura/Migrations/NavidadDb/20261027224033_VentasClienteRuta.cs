using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class VentasClienteRuta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VentaClienteRuta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    SalidaRutaId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Pagado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Anulada = table.Column<bool>(type: "bit", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_VentaClienteRuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaClienteRuta_ClienteNav_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClienteNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaClienteRuta_SalidaRuta_SalidaRutaId",
                        column: x => x.SalidaRutaId,
                        principalTable: "SalidaRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaClienteRuta_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaClienteRuta_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CobroClienteRuta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaClienteRutaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Medio = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_CobroClienteRuta", x => x.Id);
                    table.CheckConstraint("CK_CobroClienteRuta_MontoPositivo", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_CobroClienteRuta_VentaClienteRuta_VentaClienteRutaId",
                        column: x => x.VentaClienteRutaId,
                        principalTable: "VentaClienteRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaClienteRutaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaClienteRutaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_VentaClienteRutaDetalle", x => x.Id);
                    table.CheckConstraint("CK_VentaClienteRutaDetalle_PrecioNoNegativo", "[PrecioUnitario] >= 0");
                    table.CheckConstraint("CK_VentaClienteRutaDetalle_UnidadesPositivas", "[Unidades] > 0");
                    table.ForeignKey(
                        name: "FK_VentaClienteRutaDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaClienteRutaDetalle_VentaClienteRuta_VentaClienteRutaId",
                        column: x => x.VentaClienteRutaId,
                        principalTable: "VentaClienteRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobroClienteRuta_VentaClienteRutaId",
                table: "CobroClienteRuta",
                column: "VentaClienteRutaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteRuta_ClienteId",
                table: "VentaClienteRuta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteRuta_SalidaRutaId",
                table: "VentaClienteRuta",
                column: "SalidaRutaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteRuta_TemporadaId_VendedorId_Fecha",
                table: "VentaClienteRuta",
                columns: new[] { "TemporadaId", "VendedorId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteRuta_VendedorId_ClienteId",
                table: "VentaClienteRuta",
                columns: new[] { "VendedorId", "ClienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteRutaDetalle_ProductoId",
                table: "VentaClienteRutaDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaClienteRutaDetalle_VentaClienteRutaId_ProductoId",
                table: "VentaClienteRutaDetalle",
                columns: new[] { "VentaClienteRutaId", "ProductoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CobroClienteRuta");

            migrationBuilder.DropTable(
                name: "VentaClienteRutaDetalle");

            migrationBuilder.DropTable(
                name: "VentaClienteRuta");
        }
    }
}
