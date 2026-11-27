using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timeblock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstadoActivoEntidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "PROGRAMA_ACADEMICO",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "PERSONA",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "BLOQUE_CONTENIDO",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "ASIGNATURA",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activo",
                table: "PROGRAMA_ACADEMICO");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "PERSONA");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "BLOQUE_CONTENIDO");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "ASIGNATURA");
        }
    }
}
