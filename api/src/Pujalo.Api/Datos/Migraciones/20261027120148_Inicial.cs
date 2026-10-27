using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pujalo.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    categoria_padre_id = table.Column<int>(type: "int", nullable: true),
                    activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id);
                    table.CheckConstraint("ck_categoria_padre", "categoria_padre_id IS NULL OR categoria_padre_id <> id");
                    table.ForeignKey(
                        name: "FK_categoria_categoria_categoria_padre_id",
                        column: x => x.categoria_padre_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rol_id = table.Column<short>(type: "smallint", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    apellido = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    contrasena_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    cedula = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    recuperacion_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    recuperacion_expira = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuario_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "articulo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendedor_id = table.Column<long>(type: "bigint", nullable: false),
                    categoria_id = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    condicion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    permite_retiro = table.Column<bool>(type: "bit", nullable: false),
                    permite_envio = table.Column<bool>(type: "bit", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articulo", x => x.id);
                    table.CheckConstraint("ck_articulo_condicion", "condicion IN ('nuevo', 'como_nuevo', 'buen_estado', 'con_detalles', 'restaurado')");
                    table.ForeignKey(
                        name: "FK_articulo_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_articulo_usuario_vendedor_id",
                        column: x => x.vendedor_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "imagen_articulo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    articulo_id = table.Column<long>(type: "bigint", nullable: false),
                    ruta = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    orden = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imagen_articulo", x => x.id);
                    table.CheckConstraint("ck_imagen_orden", "orden BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_imagen_articulo_articulo_articulo_id",
                        column: x => x.articulo_id,
                        principalTable: "articulo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "puja",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subasta_id = table.Column<long>(type: "bigint", nullable: false),
                    postor_id = table.Column<long>(type: "bigint", nullable: false),
                    monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    creada_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    descartada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puja", x => x.id);
                    table.ForeignKey(
                        name: "FK_puja_usuario_postor_id",
                        column: x => x.postor_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subasta",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    articulo_id = table.Column<long>(type: "bigint", nullable: false),
                    precio_salida = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    incremento_minimo = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    precio_reserva = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_cierre = table.Column<DateTime>(type: "datetime2", nullable: false),
                    extender_al_final = table.Column<bool>(type: "bit", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValue: "Programada"),
                    puja_ganadora_id = table.Column<long>(type: "bigint", nullable: true),
                    cerrada_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    plazo_pago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pago_confirmado_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subasta", x => x.id);
                    table.CheckConstraint("ck_subasta_estado", "estado IN ('Programada', 'Activa', 'Finalizada', 'Desierta', 'Cancelada')");
                    table.CheckConstraint("ck_subasta_fechas", "fecha_cierre > fecha_inicio");
                    table.ForeignKey(
                        name: "FK_subasta_articulo_articulo_id",
                        column: x => x.articulo_id,
                        principalTable: "articulo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subasta_puja_puja_ganadora_id",
                        column: x => x.puja_ganadora_id,
                        principalTable: "puja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "categoria",
                columns: new[] { "id", "activa", "categoria_padre_id", "nombre" },
                values: new object[,]
                {
                    { 1, true, null, "Celulares" },
                    { 2, true, null, "Electrónica" },
                    { 3, true, null, "Videojuegos" },
                    { 4, true, null, "Instrumentos" },
                    { 5, true, null, "Hogar y muebles" },
                    { 6, true, null, "Deportes" },
                    { 7, true, null, "Relojes" },
                    { 8, true, null, "Fotografía" },
                    { 9, true, null, "Coleccionables" }
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[,]
                {
                    { (short)1, "Puede publicar artículos y pujar en subastas de otros.", "usuario" },
                    { (short)2, "Gestiona usuarios, artículos, subastas y categorías.", "administrador" }
                });

            migrationBuilder.InsertData(
                table: "categoria",
                columns: new[] { "id", "activa", "categoria_padre_id", "nombre" },
                values: new object[,]
                {
                    { 101, true, 1, "iPhone" },
                    { 102, true, 1, "Android" },
                    { 103, true, 1, "Accesorios" },
                    { 201, true, 2, "Audio" },
                    { 202, true, 2, "Monitores" },
                    { 203, true, 2, "Computadoras" },
                    { 301, true, 3, "Consolas" },
                    { 302, true, 3, "Juegos" },
                    { 303, true, 3, "Controles" },
                    { 401, true, 4, "Guitarras acústicas" },
                    { 402, true, 4, "Guitarras eléctricas" },
                    { 403, true, 4, "Teclados" },
                    { 404, true, 4, "Percusión" },
                    { 501, true, 5, "Muebles" },
                    { 502, true, 5, "Decoración" },
                    { 503, true, 5, "Cocina" },
                    { 601, true, 6, "Bicicletas" },
                    { 602, true, 6, "Gimnasio" },
                    { 603, true, 6, "Pesca" },
                    { 701, true, 7, "Digitales" },
                    { 702, true, 7, "Análogos" },
                    { 703, true, 7, "Inteligentes" },
                    { 801, true, 8, "Cámaras de rollo" },
                    { 802, true, 8, "Cámaras digitales" },
                    { 803, true, 8, "Lentes" },
                    { 804, true, 8, "Accesorios" },
                    { 901, true, 9, "Monedas" },
                    { 902, true, 9, "Sellos" },
                    { 903, true, 9, "Figuras" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_categoria_id",
                table: "articulo",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_articulo_vendedor_id",
                table: "articulo",
                column: "vendedor_id");

            migrationBuilder.CreateIndex(
                name: "IX_categoria_categoria_padre_id",
                table: "categoria",
                column: "categoria_padre_id");

            migrationBuilder.CreateIndex(
                name: "IX_categoria_nombre_categoria_padre_id",
                table: "categoria",
                columns: new[] { "nombre", "categoria_padre_id" },
                unique: true,
                filter: "[categoria_padre_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_imagen_articulo_articulo_id_orden",
                table: "imagen_articulo",
                columns: new[] { "articulo_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_puja_postor_id_subasta_id",
                table: "puja",
                columns: new[] { "postor_id", "subasta_id" });

            migrationBuilder.CreateIndex(
                name: "IX_puja_subasta_id_monto",
                table: "puja",
                columns: new[] { "subasta_id", "monto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rol_nombre",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subasta_articulo_id",
                table: "subasta",
                column: "articulo_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subasta_estado_fecha_cierre",
                table: "subasta",
                columns: new[] { "estado", "fecha_cierre" });

            migrationBuilder.CreateIndex(
                name: "IX_subasta_puja_ganadora_id",
                table: "subasta",
                column: "puja_ganadora_id",
                unique: true,
                filter: "[puja_ganadora_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_cedula",
                table: "usuario",
                column: "cedula",
                unique: true,
                filter: "[cedula] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_id",
                table: "usuario",
                column: "rol_id");

            migrationBuilder.AddForeignKey(
                name: "FK_puja_subasta_subasta_id",
                table: "puja",
                column: "subasta_id",
                principalTable: "subasta",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articulo_categoria_categoria_id",
                table: "articulo");

            migrationBuilder.DropForeignKey(
                name: "FK_articulo_usuario_vendedor_id",
                table: "articulo");

            migrationBuilder.DropForeignKey(
                name: "FK_puja_usuario_postor_id",
                table: "puja");

            migrationBuilder.DropForeignKey(
                name: "FK_subasta_articulo_articulo_id",
                table: "subasta");

            migrationBuilder.DropForeignKey(
                name: "FK_puja_subasta_subasta_id",
                table: "puja");

            migrationBuilder.DropTable(
                name: "imagen_articulo");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "articulo");

            migrationBuilder.DropTable(
                name: "subasta");

            migrationBuilder.DropTable(
                name: "puja");
        }
    }
}
