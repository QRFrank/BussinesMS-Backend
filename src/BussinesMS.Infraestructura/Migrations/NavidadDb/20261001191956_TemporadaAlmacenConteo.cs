using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class TemporadaAlmacenConteo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporadaAlmacenConteo",
                columns: table => new
                {
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporadaAlmacenConteo", x => new { x.TemporadaId, x.AlmacenId });
                    table.ForeignKey(
                        name: "FK_TemporadaAlmacenConteo_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporadaAlmacenConteo_AlmacenId",
                table: "TemporadaAlmacenConteo",
                column: "AlmacenId");

            // Migra la tienda principal existente como una tienda con conteo diario
            migrationBuilder.Sql(@"INSERT INTO TemporadaAlmacenConteo (TemporadaId, AlmacenId, CreatedAt, CreatedByUsuarioId)
                SELECT Id, TiendaPrincipalAlmacenId, GETUTCDATE(), ISNULL(UpdatedByUsuarioId, CreatedByUsuarioId)
                FROM Temporada WHERE TiendaPrincipalAlmacenId IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "TiendaPrincipalAlmacenId",
                table: "Temporada");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TiendaPrincipalAlmacenId",
                table: "Temporada",
                type: "int",
                nullable: true);

            // Restaura una tienda (la de menor AlmacenId) como tienda principal
            migrationBuilder.Sql(@"UPDATE t SET TiendaPrincipalAlmacenId = c.AlmacenId FROM Temporada t
                CROSS APPLY (SELECT MIN(AlmacenId) AS AlmacenId FROM TemporadaAlmacenConteo WHERE TemporadaId = t.Id) c;");

            migrationBuilder.DropTable(
                name: "TemporadaAlmacenConteo");
        }
    }
}
