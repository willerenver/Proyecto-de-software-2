using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMejorPostor.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class CorreoPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "correo_pendiente",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    para = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    intentos = table.Column<int>(type: "int", nullable: false),
                    proximo_intento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_correo_pendiente", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_correo_pendiente_proximo_intento",
                table: "correo_pendiente",
                column: "proximo_intento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "correo_pendiente");
        }
    }
}
