using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class CierreCajaTienda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "CobroVenta",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CierreCajaTienda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    ConteoAperturaId = table.Column<int>(type: "int", nullable: false),
                    ConteoCierreId = table.Column<int>(type: "int", nullable: false),
                    Desde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalVendido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EfectivoEsperado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Transferencias = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EfectivoEntregado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Diferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CierreCajaTienda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CierreCajaTienda_ConteoInventario_ConteoAperturaId",
                        column: x => x.ConteoAperturaId,
                        principalTable: "ConteoInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CierreCajaTienda_ConteoInventario_ConteoCierreId",
                        column: x => x.ConteoCierreId,
                        principalTable: "ConteoInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CierreCajaTienda_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobroVenta_AlmacenId",
                table: "CobroVenta",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_CierreCajaTienda_ConteoAperturaId",
                table: "CierreCajaTienda",
                column: "ConteoAperturaId");

            migrationBuilder.CreateIndex(
                name: "IX_CierreCajaTienda_ConteoCierreId",
                table: "CierreCajaTienda",
                column: "ConteoCierreId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CierreCajaTienda_TemporadaId_AlmacenId",
                table: "CierreCajaTienda",
                columns: new[] { "TemporadaId", "AlmacenId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CierreCajaTienda");

            migrationBuilder.DropIndex(
                name: "IX_CobroVenta_AlmacenId",
                table: "CobroVenta");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "CobroVenta");
        }
    }
}
