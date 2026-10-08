using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.AuthDb
{
    /// <inheritdoc />
    public partial class AgregarUsuarioSistemaYAlmacenSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SistemaId",
                table: "Almacenes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "UsuarioSistemas",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SistemaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioSistemas", x => new { x.UsuarioId, x.SistemaId });
                    table.ForeignKey(
                        name: "FK_UsuarioSistemas_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioSistemas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_SistemaId",
                table: "Almacenes",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioSistemas_SistemaId",
                table: "UsuarioSistemas",
                column: "SistemaId");

            // Backfill: cada usuario existente obtiene acceso a su SistemaIdDefault.
            // Los almacenes existentes quedan en SistemaId = 1 por el defaultValue de la columna.
            migrationBuilder.Sql(@"
                INSERT INTO UsuarioSistemas (UsuarioId, SistemaId)
                SELECT u.Id, u.SistemaIdDefault
                FROM Usuarios u
                WHERE EXISTS (SELECT 1 FROM Sistemas s WHERE s.Id = u.SistemaIdDefault)
                  AND NOT EXISTS (SELECT 1 FROM UsuarioSistemas us WHERE us.UsuarioId = u.Id AND us.SistemaId = u.SistemaIdDefault);");

            migrationBuilder.AddForeignKey(
                name: "FK_Almacenes_Sistemas_SistemaId",
                table: "Almacenes",
                column: "SistemaId",
                principalTable: "Sistemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Almacenes_Sistemas_SistemaId",
                table: "Almacenes");

            migrationBuilder.DropTable(
                name: "UsuarioSistemas");

            migrationBuilder.DropIndex(
                name: "IX_Almacenes_SistemaId",
                table: "Almacenes");

            migrationBuilder.DropColumn(
                name: "SistemaId",
                table: "Almacenes");
        }
    }
}
