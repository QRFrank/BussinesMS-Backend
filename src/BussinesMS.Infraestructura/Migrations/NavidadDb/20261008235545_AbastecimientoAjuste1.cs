using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class AbastecimientoAjuste1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TrabajaConPedido",
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

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCompraUnidad",
                table: "PedidoDetalle",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotalProveedor",
                table: "Pedido",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // A1.1: SOALPRO (1) y CARSA (2) de la temporada abierta trabajan con pedido
            migrationBuilder.Sql(@"UPDATE p SET p.TrabajaConPedido = 1
FROM Proveedor p
INNER JOIN Temporada t ON t.Id = p.TemporadaId
WHERE t.Estado = 1 AND p.Id IN (1, 2) AND p.Nombre IN ('SOALPRO', 'CARSA');");

            // Backfill de pedidos existentes: precio de la línea = precio actual del producto
            migrationBuilder.Sql(@"UPDATE d SET d.PrecioCompraUnidad = pr.PrecioCompraUnidad, d.PrecioCatalogo = pr.PrecioCatalogo
FROM PedidoDetalle d
INNER JOIN Producto pr ON pr.Id = d.ProductoId;");

            // MontoTotalProveedor = monto calculado (Σ cantidad × precio de compra)
            migrationBuilder.Sql(@"UPDATE p SET p.MontoTotalProveedor = ISNULL((
    SELECT ROUND(SUM(d.CantidadUnidades * d.PrecioCompraUnidad), 2)
    FROM PedidoDetalle d WHERE d.PedidoId = p.Id), 0)
FROM Pedido p;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrabajaConPedido",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "PrecioCatalogo",
                table: "PedidoDetalle");

            migrationBuilder.DropColumn(
                name: "PrecioCompraUnidad",
                table: "PedidoDetalle");

            migrationBuilder.DropColumn(
                name: "MontoTotalProveedor",
                table: "Pedido");
        }
    }
}
