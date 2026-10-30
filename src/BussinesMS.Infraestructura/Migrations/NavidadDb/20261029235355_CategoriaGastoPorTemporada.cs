using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class CategoriaGastoPorTemporada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriaGastoTemporadaNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoriaGastoId = table.Column<int>(type: "int", nullable: false),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CategoriaGastoTemporadaNav", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriaGastoTemporadaNav_CategoriaGastoNav_CategoriaGastoId",
                        column: x => x.CategoriaGastoId,
                        principalTable: "CategoriaGastoNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoriaGastoTemporadaNav_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaGastoTemporadaNav_CategoriaGastoId_TemporadaId",
                table: "CategoriaGastoTemporadaNav",
                columns: new[] { "CategoriaGastoId", "TemporadaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaGastoTemporadaNav_TemporadaId",
                table: "CategoriaGastoTemporadaNav",
                column: "TemporadaId");

            // Liga todas las categorías activas existentes a todas las temporadas existentes
            migrationBuilder.Sql(@"
INSERT INTO CategoriaGastoTemporadaNav (CategoriaGastoId, TemporadaId, IsActive, CreatedAt, CreatedByUsuarioId)
SELECT c.Id, t.Id, 1, SYSUTCDATETIME(), 1
FROM CategoriaGastoNav c
CROSS JOIN Temporada t
WHERE c.IsActive = 1 AND t.IsActive = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriaGastoTemporadaNav");
        }
    }
}
