using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class TemporadaAlmacenEncargado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporadaAlmacenEncargado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TemporadaAlmacenEncargado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporadaAlmacenEncargado_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporadaAlmacenEncargado_TemporadaId_UsuarioId",
                table: "TemporadaAlmacenEncargado",
                columns: new[] { "TemporadaId", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "UX_TemporadaAlmacenEncargado",
                table: "TemporadaAlmacenEncargado",
                columns: new[] { "TemporadaId", "AlmacenId", "UsuarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporadaAlmacenEncargado");
        }
    }
}
