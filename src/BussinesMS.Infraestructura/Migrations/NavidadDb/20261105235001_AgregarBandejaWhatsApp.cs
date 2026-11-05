using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class AgregarBandejaWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigAvisoNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Evento = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HoraProgramada = table.Column<TimeOnly>(type: "time", nullable: true),
                    Parametro = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ConfigAvisoNav", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificacionNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: true),
                    Evento = table.Column<int>(type: "int", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Audiencia = table.Column<int>(type: "int", nullable: false),
                    AudienciaId = table.Column<int>(type: "int", nullable: true),
                    TelefonoDirecto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExcluirUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AdjuntoTipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReferenciaId = table.Column<int>(type: "int", nullable: true),
                    ClaveUnica = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProgramadaPara = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResueltaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_NotificacionNav", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificacionEnvioNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DestinatarioDescripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    UltimoError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnviadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProximoIntentoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendienteDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstanciaUsada = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_NotificacionEnvioNav", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionEnvioNav_NotificacionNav_NotificacionId",
                        column: x => x.NotificacionId,
                        principalTable: "NotificacionNav",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ConfigAvisoNav_Evento",
                table: "ConfigAvisoNav",
                column: "Evento",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionEnvioNav_Estado_Enviado",
                table: "NotificacionEnvioNav",
                columns: new[] { "Estado", "EnviadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionEnvioNav_Estado_Proximo",
                table: "NotificacionEnvioNav",
                columns: new[] { "Estado", "ProximoIntentoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionEnvioNav_NotificacionId",
                table: "NotificacionEnvioNav",
                column: "NotificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionNav_Evento",
                table: "NotificacionNav",
                column: "Evento");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionNav_ResueltaEn",
                table: "NotificacionNav",
                column: "ResueltaEn");

            migrationBuilder.CreateIndex(
                name: "UX_NotificacionNav_ClaveUnica",
                table: "NotificacionNav",
                column: "ClaveUnica",
                unique: true,
                filter: "[ClaveUnica] IS NOT NULL AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigAvisoNav");

            migrationBuilder.DropTable(
                name: "NotificacionEnvioNav");

            migrationBuilder.DropTable(
                name: "NotificacionNav");
        }
    }
}
