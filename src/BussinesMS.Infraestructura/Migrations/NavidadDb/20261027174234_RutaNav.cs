using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class RutaNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbonoVendedorId",
                table: "CobroVenta",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AbonoVendedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Medio = table.Column<int>(type: "int", nullable: true),
                    Comprobante = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_AbonoVendedor", x => x.Id);
                    table.CheckConstraint("CK_AbonoVendedor_Montos", "[Monto] >= 0 AND [Descuento] >= 0 AND [Monto] + [Descuento] > 0");
                    table.ForeignKey(
                        name: "FK_AbonoVendedor_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbonoVendedor_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalidaRuta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VentaId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SalidaRuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalidaRuta_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalidaRuta_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalidaRuta_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbonoVendedorAplicacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AbonoVendedorId = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    MontoCobrado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_AbonoVendedorAplicacion", x => x.Id);
                    table.CheckConstraint("CK_AbonoVendedorAplicacion_Montos", "[MontoCobrado] >= 0 AND [MontoDescuento] >= 0 AND [MontoCobrado] + [MontoDescuento] > 0");
                    table.ForeignKey(
                        name: "FK_AbonoVendedorAplicacion_AbonoVendedor_AbonoVendedorId",
                        column: x => x.AbonoVendedorId,
                        principalTable: "AbonoVendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbonoVendedorAplicacion_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionRuta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalidaRutaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    EsTardia = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_DevolucionRuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionRuta_SalidaRuta_SalidaRutaId",
                        column: x => x.SalidaRutaId,
                        principalTable: "SalidaRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalidaRutaCarga",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalidaRutaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Anulada = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SalidaRutaCarga", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalidaRutaCarga_SalidaRuta_SalidaRutaId",
                        column: x => x.SalidaRutaId,
                        principalTable: "SalidaRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionRutaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    AlmacenDestinoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DevolucionRutaDetalle", x => x.Id);
                    table.CheckConstraint("CK_DevolucionRutaDetalle_UnidadesPositivas", "[Unidades] > 0");
                    table.ForeignKey(
                        name: "FK_DevolucionRutaDetalle_DevolucionRuta_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "DevolucionRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevolucionRutaDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalidaRutaCargaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaId = table.Column<int>(type: "int", nullable: false),
                    AlmacenOrigenId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SalidaRutaCargaDetalle", x => x.Id);
                    table.CheckConstraint("CK_SalidaRutaCargaDetalle_UnidadesPositivas", "[Unidades] > 0");
                    table.ForeignKey(
                        name: "FK_SalidaRutaCargaDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalidaRutaCargaDetalle_SalidaRutaCarga_CargaId",
                        column: x => x.CargaId,
                        principalTable: "SalidaRutaCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobroVenta_AbonoVendedorId",
                table: "CobroVenta",
                column: "AbonoVendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonoVendedor_TemporadaId_VendedorId",
                table: "AbonoVendedor",
                columns: new[] { "TemporadaId", "VendedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbonoVendedor_VendedorId",
                table: "AbonoVendedor",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonoVendedorAplicacion_AbonoVendedorId",
                table: "AbonoVendedorAplicacion",
                column: "AbonoVendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonoVendedorAplicacion_VentaId",
                table: "AbonoVendedorAplicacion",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionRuta_SalidaRutaId",
                table: "DevolucionRuta",
                column: "SalidaRutaId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionRutaDetalle_DevolucionId_AlmacenDestinoId_ProductoId",
                table: "DevolucionRutaDetalle",
                columns: new[] { "DevolucionId", "AlmacenDestinoId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionRutaDetalle_ProductoId",
                table: "DevolucionRutaDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRuta_TemporadaId_VendedorId",
                table: "SalidaRuta",
                columns: new[] { "TemporadaId", "VendedorId" },
                unique: true,
                filter: "[IsActive] = 1 AND [Estado] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRuta_TemporadaId_VendedorId_Estado",
                table: "SalidaRuta",
                columns: new[] { "TemporadaId", "VendedorId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRuta_VendedorId",
                table: "SalidaRuta",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRuta_VentaId",
                table: "SalidaRuta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRutaCarga_SalidaRutaId",
                table: "SalidaRutaCarga",
                column: "SalidaRutaId");

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRutaCargaDetalle_CargaId_AlmacenOrigenId_ProductoId",
                table: "SalidaRutaCargaDetalle",
                columns: new[] { "CargaId", "AlmacenOrigenId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidaRutaCargaDetalle_ProductoId",
                table: "SalidaRutaCargaDetalle",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_CobroVenta_AbonoVendedor_AbonoVendedorId",
                table: "CobroVenta",
                column: "AbonoVendedorId",
                principalTable: "AbonoVendedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CobroVenta_AbonoVendedor_AbonoVendedorId",
                table: "CobroVenta");

            migrationBuilder.DropTable(
                name: "AbonoVendedorAplicacion");

            migrationBuilder.DropTable(
                name: "DevolucionRutaDetalle");

            migrationBuilder.DropTable(
                name: "SalidaRutaCargaDetalle");

            migrationBuilder.DropTable(
                name: "AbonoVendedor");

            migrationBuilder.DropTable(
                name: "DevolucionRuta");

            migrationBuilder.DropTable(
                name: "SalidaRutaCarga");

            migrationBuilder.DropTable(
                name: "SalidaRuta");

            migrationBuilder.DropIndex(
                name: "IX_CobroVenta_AbonoVendedorId",
                table: "CobroVenta");

            migrationBuilder.DropColumn(
                name: "AbonoVendedorId",
                table: "CobroVenta");
        }
    }
}
