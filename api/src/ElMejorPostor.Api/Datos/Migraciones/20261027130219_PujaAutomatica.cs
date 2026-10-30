using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElMejorPostor.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class PujaAutomatica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "automatica",
                table: "puja",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "puja_automatica",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subasta_id = table.Column<long>(type: "bigint", nullable: false),
                    postor_id = table.Column<long>(type: "bigint", nullable: false),
                    maximo = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    creada_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puja_automatica", x => x.id);
                    table.ForeignKey(
                        name: "FK_puja_automatica_subasta_subasta_id",
                        column: x => x.subasta_id,
                        principalTable: "subasta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_puja_automatica_usuario_postor_id",
                        column: x => x.postor_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_puja_automatica_postor_id",
                table: "puja_automatica",
                column: "postor_id");

            migrationBuilder.CreateIndex(
                name: "IX_puja_automatica_subasta_id_postor_id",
                table: "puja_automatica",
                columns: new[] { "subasta_id", "postor_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "puja_automatica");

            migrationBuilder.DropColumn(
                name: "automatica",
                table: "puja");
        }
    }
}
