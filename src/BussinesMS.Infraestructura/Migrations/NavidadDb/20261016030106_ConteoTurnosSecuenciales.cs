using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class ConteoTurnosSecuenciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ConteoInventario_Temporada_Almacen_Fecha_Tipo",
                table: "ConteoInventario");

            migrationBuilder.CreateIndex(
                name: "IX_ConteoInventario_Temporada_Almacen_Fecha_Tipo",
                table: "ConteoInventario",
                columns: new[] { "TemporadaId", "AlmacenId", "Fecha", "Tipo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConteoInventario_Temporada_Almacen_Fecha_Tipo",
                table: "ConteoInventario");

            migrationBuilder.CreateIndex(
                name: "UX_ConteoInventario_Temporada_Almacen_Fecha_Tipo",
                table: "ConteoInventario",
                columns: new[] { "TemporadaId", "AlmacenId", "Fecha", "Tipo" },
                unique: true);
        }
    }
}
