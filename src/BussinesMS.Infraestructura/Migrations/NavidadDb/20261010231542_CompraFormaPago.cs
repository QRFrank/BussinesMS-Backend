using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class CompraFormaPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormaPago",
                table: "Compra",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: 1 Contado | 3 Credito + pago inicial (aunque su pago este anulado) | 2 Credito
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.FormaPago = CASE
                    WHEN c.PagadaAlContado = 1 THEN 1
                    WHEN EXISTS (SELECT 1 FROM PagoProveedor p
                                 WHERE p.CompraId = c.Id AND p.Observacion = N'Pago inicial de compra a crédito') THEN 3
                    ELSE 2 END
                FROM Compra c;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormaPago",
                table: "Compra");
        }
    }
}
