using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoLoteToInventarioLote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoLote",
                table: "InventarioLotes",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UQ_InventarioLote_CodigoLote",
                table: "InventarioLotes",
                column: "CodigoLote",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_InventarioLote_CodigoLote",
                table: "InventarioLotes");

            migrationBuilder.DropColumn(
                name: "CodigoLote",
                table: "InventarioLotes");
        }
    }
}
