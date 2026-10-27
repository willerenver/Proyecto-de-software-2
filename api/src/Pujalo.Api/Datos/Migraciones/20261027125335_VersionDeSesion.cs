using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pujalo.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class VersionDeSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version_sesion",
                table: "usuario",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version_sesion",
                table: "usuario");
        }
    }
}
