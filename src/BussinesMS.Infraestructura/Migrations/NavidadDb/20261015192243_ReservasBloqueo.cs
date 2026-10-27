using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class ReservasBloqueo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloquearStock",
                table: "VentaNav");

            migrationBuilder.AlterColumn<int>(
                name: "AlmacenId",
                table: "VentaNav",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "ReservaBloqueo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    VentaDetalleId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
                    RecepcionId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ReservaBloqueo", x => x.Id);
                    table.CheckConstraint("CK_ReservaBloqueo_UnidadesPositivas", "[Unidades] > 0");
                    table.ForeignKey(
                        name: "FK_ReservaBloqueo_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaBloqueo_Recepcion_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaBloqueo_VentaNavDetalle_VentaDetalleId",
                        column: x => x.VentaDetalleId,
                        principalTable: "VentaNavDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaBloqueo_VentaNav_VentaId",
                        column: x => x.VentaId,
                        principalTable: "VentaNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaBloqueo_AlmacenId_ProductoId",
                table: "ReservaBloqueo",
                columns: new[] { "AlmacenId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaBloqueo_ProductoId",
                table: "ReservaBloqueo",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaBloqueo_RecepcionId",
                table: "ReservaBloqueo",
                column: "RecepcionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaBloqueo_VentaDetalleId",
                table: "ReservaBloqueo",
                column: "VentaDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaBloqueo_VentaId",
                table: "ReservaBloqueo",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservaBloqueo");

            migrationBuilder.AlterColumn<int>(
                name: "AlmacenId",
                table: "VentaNav",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BloquearStock",
                table: "VentaNav",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
