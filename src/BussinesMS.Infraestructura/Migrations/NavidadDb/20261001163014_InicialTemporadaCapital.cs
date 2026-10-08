using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class InicialTemporadaCapital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriaGastoNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_CategoriaGastoNav", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inversor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Inversor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Temporada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "date", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "date", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    TiendaPrincipalAlmacenId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Temporada", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AporteCapital",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    InversorId = table.Column<int>(type: "int", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    PorcentajeComision = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AporteCapital", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AporteCapital_Inversor_InversorId",
                        column: x => x.InversorId,
                        principalTable: "Inversor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AporteCapital_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GastoNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_GastoNav", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GastoNav_CategoriaGastoNav_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriaGastoNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GastoNav_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagoInversor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    AporteCapitalId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PagoInversor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoInversor_AporteCapital_AporteCapitalId",
                        column: x => x.AporteCapitalId,
                        principalTable: "AporteCapital",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoInversor_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AporteCapital_InversorId",
                table: "AporteCapital",
                column: "InversorId");

            migrationBuilder.CreateIndex(
                name: "IX_AporteCapital_TemporadaId",
                table: "AporteCapital",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaGastoNav_Nombre",
                table: "CategoriaGastoNav",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GastoNav_CategoriaId",
                table: "GastoNav",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GastoNav_Fecha",
                table: "GastoNav",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_GastoNav_TemporadaId",
                table: "GastoNav",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inversor_Nombre",
                table: "Inversor",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_PagoInversor_AporteCapitalId",
                table: "PagoInversor",
                column: "AporteCapitalId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoInversor_TemporadaId",
                table: "PagoInversor",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Temporada_Anio",
                table: "Temporada",
                column: "Anio");

            migrationBuilder.CreateIndex(
                name: "UX_Temporada_UnaAbierta",
                table: "Temporada",
                column: "Estado",
                unique: true,
                filter: "[Estado] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GastoNav");

            migrationBuilder.DropTable(
                name: "PagoInversor");

            migrationBuilder.DropTable(
                name: "CategoriaGastoNav");

            migrationBuilder.DropTable(
                name: "AporteCapital");

            migrationBuilder.DropTable(
                name: "Inversor");

            migrationBuilder.DropTable(
                name: "Temporada");
        }
    }
}
