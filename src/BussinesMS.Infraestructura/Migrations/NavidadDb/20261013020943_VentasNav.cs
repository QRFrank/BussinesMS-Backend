using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class VentasNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotivoDiferencia",
                table: "ConteoInventarioDetalle",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsumoNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ConsumoNav", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumoNav_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    NroVenta = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    Canal = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    CondicionPago = table.Column<int>(type: "int", nullable: false),
                    BloquearStock = table.Column<bool>(type: "bit", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoNeto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoCobrado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoSaldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstadoPago = table.Column<int>(type: "int", nullable: false),
                    EstadoEntrega = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_VentaNav", x => x.Id);
                    table.CheckConstraint("CK_VentaNav_CobradoMenorIgualNeto", "[MontoCobrado] <= [MontoNeto]");
                    table.CheckConstraint("CK_VentaNav_MontosNoNegativos", "[MontoTotal] >= 0 AND [Descuento] >= 0 AND [MontoNeto] >= 0 AND [MontoCobrado] >= 0 AND [MontoSaldo] >= 0");
                    table.ForeignKey(
                        name: "FK_VentaNav_ClienteNav_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClienteNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaNav_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaNav_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumoNavDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumoId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ConsumoNavDetalle", x => x.Id);
                    table.CheckConstraint("CK_ConsumoNavDetalle_UnidadesPositivas", "[Unidades] > 0");
                    table.ForeignKey(
                        name: "FK_ConsumoNavDetalle_ConsumoNav_ConsumoId",
                        column: x => x.ConsumoId,
                        principalTable: "ConsumoNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumoNavDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CobroVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Medio = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CobroVenta", x => x.Id);
                    table.CheckConstraint("CK_CobroVenta_MontoPositivo", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_CobroVenta_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_EntregaVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaVenta_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VentaNavDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnidadesEntregadas = table.Column<int>(type: "int", nullable: false),
                    EstadoEntrega = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_VentaNavDetalle", x => x.Id);
                    table.CheckConstraint("CK_VentaNavDetalle_EntregadasEnRango", "[UnidadesEntregadas] >= 0 AND [UnidadesEntregadas] <= [Unidades]");
                    table.CheckConstraint("CK_VentaNavDetalle_MontosNoNegativos", "[Unidades] >= 0 AND [PrecioUnitario] >= 0 AND [Subtotal] >= 0");
                    table.ForeignKey(
                        name: "FK_VentaNavDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaNavDetalle_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaVentaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntregaId = table.Column<int>(type: "int", nullable: false),
                    VentaDetalleId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EntregaVentaDetalle", x => x.Id);
                    table.CheckConstraint("CK_EntregaVentaDetalle_UnidadesNoNegativas", "[Unidades] >= 0");
                    table.ForeignKey(
                        name: "FK_EntregaVentaDetalle_EntregaVenta_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "EntregaVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntregaVentaDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntregaVentaDetalle_VentaNavDetalle_VentaDetalleId",
                        column: x => x.VentaDetalleId,
                        principalTable: "VentaNavDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobroVenta_VentaId",
                table: "CobroVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoNav_AlmacenId",
                table: "ConsumoNav",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoNav_Fecha",
                table: "ConsumoNav",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoNav_TemporadaId",
                table: "ConsumoNav",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoNavDetalle_ConsumoId_ProductoId",
                table: "ConsumoNavDetalle",
                columns: new[] { "ConsumoId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoNavDetalle_ProductoId",
                table: "ConsumoNavDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaVenta_AlmacenId",
                table: "EntregaVenta",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaVenta_VentaId",
                table: "EntregaVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaVentaDetalle_EntregaId",
                table: "EntregaVentaDetalle",
                column: "EntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaVentaDetalle_ProductoId",
                table: "EntregaVentaDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaVentaDetalle_VentaDetalleId",
                table: "EntregaVentaDetalle",
                column: "VentaDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaNav_AlmacenId",
                table: "VentaNav",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaNav_ClienteId",
                table: "VentaNav",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaNav_Fecha",
                table: "VentaNav",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_VentaNav_VendedorId",
                table: "VentaNav",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "UX_VentaNav_Temporada_NroVenta",
                table: "VentaNav",
                columns: new[] { "TemporadaId", "NroVenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaNavDetalle_ProductoId",
                table: "VentaNavDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaNavDetalle_VentaId_ProductoId",
                table: "VentaNavDetalle",
                columns: new[] { "VentaId", "ProductoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CobroVenta");

            migrationBuilder.DropTable(
                name: "ConsumoNavDetalle");

            migrationBuilder.DropTable(
                name: "EntregaVentaDetalle");

            migrationBuilder.DropTable(
                name: "ConsumoNav");

            migrationBuilder.DropTable(
                name: "EntregaVenta");

            migrationBuilder.DropTable(
                name: "VentaNavDetalle");

            migrationBuilder.DropTable(
                name: "VentaNav");

            migrationBuilder.DropColumn(
                name: "MotivoDiferencia",
                table: "ConteoInventarioDetalle");
        }
    }
}
