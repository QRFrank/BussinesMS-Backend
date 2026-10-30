using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.AuthDb
{
    /// <inheritdoc />
    public partial class AgregarPermisoAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermisoAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SistemaId = table.Column<int>(type: "int", nullable: true),
                    Metodo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Ruta = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Menus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FlagRequerido = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Modo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisoAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermisoAuditoria_Fecha",
                table: "PermisoAuditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PermisoAuditoria_UsuarioId_Endpoint",
                table: "PermisoAuditoria",
                columns: new[] { "UsuarioId", "Endpoint" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermisoAuditoria");
        }
    }
}
