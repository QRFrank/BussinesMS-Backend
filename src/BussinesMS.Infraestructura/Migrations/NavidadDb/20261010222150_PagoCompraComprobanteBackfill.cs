using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class PagoCompraComprobanteBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: pagos automáticos de compra sin comprobante (nº de nota, o "Compra #id")
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Comprobante = COALESCE(NULLIF(LTRIM(RTRIM(c.NroNota)), ''), 'Compra #' + CAST(c.Id AS varchar(20)))
                FROM PagoProveedor p
                INNER JOIN Compra c ON c.PagoProveedorId = p.Id
                WHERE p.Comprobante IS NULL OR LTRIM(RTRIM(p.Comprobante)) = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
