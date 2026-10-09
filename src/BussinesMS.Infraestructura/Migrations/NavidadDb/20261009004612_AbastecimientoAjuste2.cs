using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class AbastecimientoAjuste2 : Migration
    {
        /// <inheritdoc />
        // Revisado a mano (Ajuste 2). Orden: 1) tablas nuevas e índices, 2) migración de datos,
        // 3) recién después se borran LoteAlmacen, MovimientoNav.LoteId, Proveedor.PreciosFijos y PedidoDetalle.PrecioCatalogo.
        // Al generarla (2026-10-08) las tablas del abastecimiento estaban vacías; la conversión queda igual por si hubiera datos.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===== 1) Tablas nuevas =====
            migrationBuilder.CreateTable(
                name: "Compra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    NroNota = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    PagadaAlContado = table.Column<bool>(type: "bit", nullable: false),
                    PagoProveedorId = table.Column<int>(type: "int", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Compra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compra_PagoProveedor_PagoProveedorId",
                        column: x => x.PagoProveedorId,
                        principalTable: "PagoProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compra_Proveedor_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compra_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAlmacen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StockAlmacen", x => x.Id);
                    table.CheckConstraint("CK_StockAlmacen_CantidadNoNegativa", "[Cantidad] >= 0");
                    table.ForeignKey(
                        name: "FK_StockAlmacen_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAlmacen_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompraDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompraId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    CantidadUnidades = table.Column<int>(type: "int", nullable: false),
                    PrecioCompraUnidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_CompraDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraDetalle_Compra_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompraDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompraDistribucion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    CantidadUnidades = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CompraDistribucion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraDistribucion_CompraDetalle_CompraDetalleId",
                        column: x => x.CompraDetalleId,
                        principalTable: "CompraDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Pedido_Proveedor_Codigo",
                table: "Pedido",
                columns: new[] { "ProveedorId", "CodigoClienteId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_Fecha",
                table: "Compra",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_PagoProveedorId",
                table: "Compra",
                column: "PagoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_ProveedorId",
                table: "Compra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_TemporadaId",
                table: "Compra",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_CompraId_ProductoId",
                table: "CompraDetalle",
                columns: new[] { "CompraId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_ProductoId",
                table: "CompraDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDistribucion_CompraDetalleId_AlmacenId",
                table: "CompraDistribucion",
                columns: new[] { "CompraDetalleId", "AlmacenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAlmacen_AlmacenId",
                table: "StockAlmacen",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlmacen_TemporadaId",
                table: "StockAlmacen",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "UX_StockAlmacen_Producto_Almacen",
                table: "StockAlmacen",
                columns: new[] { "ProductoId", "AlmacenId" },
                unique: true);

            // ===== 2) Datos =====
            // 2a) LoteAlmacen → StockAlmacen: suma por producto × almacén (los lotes de recepciones anuladas ya están en 0)
            migrationBuilder.Sql(@"
INSERT INTO [StockAlmacen] ([TemporadaId], [ProductoId], [AlmacenId], [Cantidad], [IsActive], [CreatedAt], [CreatedByUsuarioId])
SELECT p.[TemporadaId], la.[ProductoId], la.[AlmacenId], SUM(la.[StockDisponible]), 1, SYSUTCDATETIME(), 1
FROM [LoteAlmacen] la
INNER JOIN [Producto] p ON p.[Id] = la.[ProductoId]
GROUP BY p.[TemporadaId], la.[ProductoId], la.[AlmacenId];");

            // 2b) Recepciones de proveedores sin TrabajaConPedido → compras a crédito (misma fecha, nota, anulada y auditoría).
            //     Sus movimientos pasan a referenciar la compra (Recepcion→Compra, AnulacionRecepcion→AnulacionCompra).
            //     El código de cliente (si lo tuviera) se pierde: las compras no llevan código.
            migrationBuilder.Sql(@"
DECLARE @mapR TABLE ([RecepcionId] int NOT NULL, [CompraId] int NOT NULL);
DECLARE @mapD TABLE ([DetalleId] int NOT NULL, [CompraDetalleId] int NOT NULL);

MERGE INTO [Compra] AS t
USING (
    SELECT r.* FROM [Recepcion] r
    INNER JOIN [Proveedor] pr ON pr.[Id] = r.[ProveedorId]
    WHERE pr.[TrabajaConPedido] = 0
) AS s
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT ([TemporadaId], [ProveedorId], [NroNota], [Fecha], [PagadaAlContado], [PagoProveedorId], [Observacion], [Anulada],
            [IsActive], [CreatedAt], [UpdatedAt], [DeletedAt], [CreatedByUsuarioId], [UpdatedByUsuarioId], [DeletedByUsuarioId])
    VALUES (s.[TemporadaId], s.[ProveedorId], s.[NroFactura], s.[Fecha], 0, NULL, s.[Observacion], s.[Anulada],
            s.[IsActive], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt], s.[CreatedByUsuarioId], s.[UpdatedByUsuarioId], s.[DeletedByUsuarioId])
OUTPUT s.[Id], inserted.[Id] INTO @mapR ([RecepcionId], [CompraId]);

MERGE INTO [CompraDetalle] AS t
USING (
    SELECT d.*, m.[CompraId] AS [NuevaCompraId] FROM [RecepcionDetalle] d
    INNER JOIN @mapR m ON m.[RecepcionId] = d.[RecepcionId]
) AS s
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT ([CompraId], [ProductoId], [CantidadUnidades], [PrecioCompraUnidad],
            [IsActive], [CreatedAt], [UpdatedAt], [DeletedAt], [CreatedByUsuarioId], [UpdatedByUsuarioId], [DeletedByUsuarioId])
    VALUES (s.[NuevaCompraId], s.[ProductoId], s.[CantidadUnidades], s.[PrecioCompraUnidad],
            s.[IsActive], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt], s.[CreatedByUsuarioId], s.[UpdatedByUsuarioId], s.[DeletedByUsuarioId])
OUTPUT s.[Id], inserted.[Id] INTO @mapD ([DetalleId], [CompraDetalleId]);

INSERT INTO [CompraDistribucion] ([CompraDetalleId], [AlmacenId], [CantidadUnidades],
    [IsActive], [CreatedAt], [UpdatedAt], [DeletedAt], [CreatedByUsuarioId], [UpdatedByUsuarioId], [DeletedByUsuarioId])
SELECT md.[CompraDetalleId], x.[AlmacenId], x.[CantidadUnidades],
    x.[IsActive], x.[CreatedAt], x.[UpdatedAt], x.[DeletedAt], x.[CreatedByUsuarioId], x.[UpdatedByUsuarioId], x.[DeletedByUsuarioId]
FROM [RecepcionDistribucion] x
INNER JOIN @mapD md ON md.[DetalleId] = x.[RecepcionDetalleId];

UPDATE mv SET
    mv.[ReferenciaTipo] = N'Compra',
    mv.[ReferenciaId] = m.[CompraId],
    mv.[Tipo] = CASE mv.[Tipo] WHEN 1 THEN 12 WHEN 11 THEN 13 ELSE mv.[Tipo] END
FROM [MovimientoNav] mv
INNER JOIN @mapR m ON mv.[ReferenciaTipo] = N'Recepcion' AND mv.[ReferenciaId] = m.[RecepcionId];");

            // ===== 3) Se borra lo viejo (después de copiar) =====
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoNav_RecepcionDetalle_LoteId",
                table: "MovimientoNav");

            migrationBuilder.DropTable(
                name: "LoteAlmacen");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoNav_LoteId",
                table: "MovimientoNav");

            migrationBuilder.DropColumn(
                name: "LoteId",
                table: "MovimientoNav");

            // Recepciones ya copiadas a Compra (detalles y distribución se borran en cascada).
            // Va después de quitar LoteAlmacen y MovimientoNav.LoteId, que referenciaban RecepcionDetalle.
            migrationBuilder.Sql(@"
DELETE r FROM [Recepcion] r
INNER JOIN [Proveedor] pr ON pr.[Id] = r.[ProveedorId]
WHERE pr.[TrabajaConPedido] = 0;");

            migrationBuilder.DropColumn(
                name: "PreciosFijos",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "PrecioCatalogo",
                table: "PedidoDetalle");
        }

        /// <inheritdoc />
        // Down solo restaura el esquema: no reconvierte datos (stock, compras ni precios fijos).
        // Si ya hay MovimientoNav, la FK de LoteId (default 0) falla: habría que vaciar el kardex antes.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompraDistribucion");

            migrationBuilder.DropTable(
                name: "StockAlmacen");

            migrationBuilder.DropTable(
                name: "CompraDetalle");

            migrationBuilder.DropTable(
                name: "Compra");

            migrationBuilder.DropIndex(
                name: "UX_Pedido_Proveedor_Codigo",
                table: "Pedido");

            migrationBuilder.AddColumn<bool>(
                name: "PreciosFijos",
                table: "Proveedor",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCatalogo",
                table: "PedidoDetalle",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LoteId",
                table: "MovimientoNav",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LoteAlmacen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUsuarioId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StockDisponible = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoteAlmacen", x => x.Id);
                    table.CheckConstraint("CK_LoteAlmacen_StockNoNegativo", "[StockDisponible] >= 0");
                    table.ForeignKey(
                        name: "FK_LoteAlmacen_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoteAlmacen_RecepcionDetalle_LoteId",
                        column: x => x.LoteId,
                        principalTable: "RecepcionDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoNav_LoteId",
                table: "MovimientoNav",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteAlmacen_AlmacenId_ProductoId",
                table: "LoteAlmacen",
                columns: new[] { "AlmacenId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoteAlmacen_LoteId_AlmacenId",
                table: "LoteAlmacen",
                columns: new[] { "LoteId", "AlmacenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoteAlmacen_ProductoId",
                table: "LoteAlmacen",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoNav_RecepcionDetalle_LoteId",
                table: "MovimientoNav",
                column: "LoteId",
                principalTable: "RecepcionDetalle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
