using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.AuthDb
{
    /// <inheritdoc />
    public partial class AgregarPuedeHacerRecepciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PuedeHacerRecepciones",
                table: "UsuarioSistemas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Relleno (regla #7, no cortar el trabajo): quien hoy tiene asignado el menú 132 (Recepciones)
            // del sistema navideño queda con el permiso en 1.
            migrationBuilder.Sql(@"
                UPDATE us SET PuedeHacerRecepciones = 1
                FROM UsuarioSistemas us
                WHERE us.SistemaId = 2
                  AND EXISTS (SELECT 1 FROM UsuarioMenus um WHERE um.UsuarioId = us.UsuarioId AND um.MenuId = 132);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuedeHacerRecepciones",
                table: "UsuarioSistemas");
        }
    }
}
