using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class GastoOperativoSesionOpcionalFechaAlmacen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SesionCajaId",
                table: "GastosOperativos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "GastosOperativos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaGasto",
                table: "GastosOperativos",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_GastosOperativos_AlmacenId",
                table: "GastosOperativos",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_GastosOperativos_FechaGasto",
                table: "GastosOperativos",
                column: "FechaGasto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GastosOperativos_AlmacenId",
                table: "GastosOperativos");

            migrationBuilder.DropIndex(
                name: "IX_GastosOperativos_FechaGasto",
                table: "GastosOperativos");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "GastosOperativos");

            migrationBuilder.DropColumn(
                name: "FechaGasto",
                table: "GastosOperativos");

            migrationBuilder.AlterColumn<int>(
                name: "SesionCajaId",
                table: "GastosOperativos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
