using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinesMS.Infraestructura.Migrations.NavidadDb
{
    /// <inheritdoc />
    public partial class Catalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClienteNav",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_ClienteNav", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UsaCodigosCliente = table.Column<bool>(type: "bit", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Proveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proveedor_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    SueldoMensual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TipoComision = table.Column<int>(type: "int", nullable: true),
                    PorcentajeComision = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
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
                    table.PrimaryKey("PK_Vendedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendedor_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodigoCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Titular = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_CodigoCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodigoCliente_Proveedor_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodigoCliente_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemporadaId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PrecioCompraUnidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComisionRutaPorUnidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_Producto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Producto_Proveedor_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Producto_Temporada_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductoPresentacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unidades = table.Column<int>(type: "int", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ProductoPresentacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoPresentacion_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteNav_Nombre",
                table: "ClienteNav",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UX_ClienteNav_Documento",
                table: "ClienteNav",
                column: "Documento",
                unique: true,
                filter: "[Documento] IS NOT NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CodigoCliente_TemporadaId",
                table: "CodigoCliente",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "UX_CodigoCliente_Proveedor_Codigo",
                table: "CodigoCliente",
                columns: new[] { "ProveedorId", "Codigo" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_TemporadaId",
                table: "Producto",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "UX_Producto_Proveedor_Nombre",
                table: "Producto",
                columns: new[] { "ProveedorId", "Nombre" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ProductoPresentacion_Producto_Unidades",
                table: "ProductoPresentacion",
                columns: new[] { "ProductoId", "Unidades" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_TemporadaId",
                table: "Proveedor",
                column: "TemporadaId");

            migrationBuilder.CreateIndex(
                name: "UX_Proveedor_Temporada_Nombre",
                table: "Proveedor",
                columns: new[] { "TemporadaId", "Nombre" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedor_UsuarioId",
                table: "Vendedor",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_Vendedor_Temporada_Usuario",
                table: "Vendedor",
                columns: new[] { "TemporadaId", "UsuarioId" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClienteNav");

            migrationBuilder.DropTable(
                name: "CodigoCliente");

            migrationBuilder.DropTable(
                name: "ProductoPresentacion");

            migrationBuilder.DropTable(
                name: "Vendedor");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Proveedor");
        }
    }
}
