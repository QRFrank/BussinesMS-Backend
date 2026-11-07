using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class CierreTemporadaIncobrables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoIncobrable",
                table: "VentaNav",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CierreTemporadaNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "date", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Ganancia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Devengados = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GananciaNeta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeudasIncobrables = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockSobranteUnidades = table.Column<int>(type: "int", nullable: false),
                    StockSobranteMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EfectivoNeto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NosDeben = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Debemos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TeQueda = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CapitalPropio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_CierreTemporadaNav", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CierreTemporadaNav_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncobrableNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    VendedorId = table.Column<int>(type: "int", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Anulado = table.Column<bool>(type: "bit", nullable: false),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioAnulacionId = table.Column<int>(type: "int", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_IncobrableNav", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncobrableNav_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncobrableNav_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncobrableNav_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncobrableNavAplicacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncobrableId = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_IncobrableNavAplicacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncobrableNavAplicacion_IncobrableNav_IncobrableId",
                        column: x => x.IncobrableId,
                        principalTable: "IncobrableNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncobrableNavAplicacion_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CierreTemporadaNav_TemporadaId",
                table: "CierreTemporadaNav",
                column: "TemporadaId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_IncobrableNav_TemporadaId",
                table: "IncobrableNav",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_IncobrableNav_VendedorId",
                table: "IncobrableNav",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_IncobrableNav_VentaId",
                table: "IncobrableNav",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_IncobrableNavAplicacion_IncobrableId",
                table: "IncobrableNavAplicacion",
                column: "IncobrableId");

            migrationBuilder.CreateIndex(
                name: "IX_IncobrableNavAplicacion_VentaId",
                table: "IncobrableNavAplicacion",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CierreTemporadaNav");

            migrationBuilder.DropTable(
                name: "IncobrableNavAplicacion");

            migrationBuilder.DropTable(
                name: "IncobrableNav");

            migrationBuilder.DropColumn(
                name: "MontoIncobrable",
                table: "VentaNav");
        }
    }
}
