using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuntalyColegios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PadresYVehiculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Representantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColegioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Representantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Representantes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Representantes_Colegios_ColegioId",
                        column: x => x.ColegioId,
                        principalTable: "Colegios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepresentantesAlumnos",
                columns: table => new
                {
                    AlumnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepresentanteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepresentantesAlumnos", x => new { x.RepresentanteId, x.AlumnoId });
                    table.ForeignKey(
                        name: "FK_RepresentantesAlumnos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepresentantesAlumnos_Representantes_RepresentanteId",
                        column: x => x.RepresentanteId,
                        principalTable: "Representantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepresentanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColegioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tag = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Colegios_ColegioId",
                        column: x => x.ColegioId,
                        principalTable: "Colegios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Representantes_RepresentanteId",
                        column: x => x.RepresentanteId,
                        principalTable: "Representantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Representantes_ColegioId",
                table: "Representantes",
                column: "ColegioId");

            migrationBuilder.CreateIndex(
                name: "IX_Representantes_UsuarioId",
                table: "Representantes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepresentantesAlumnos_AlumnoId",
                table: "RepresentantesAlumnos",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_ColegioId_Tag",
                table: "Vehiculos",
                columns: new[] { "ColegioId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_RepresentanteId_Placa",
                table: "Vehiculos",
                columns: new[] { "RepresentanteId", "Placa" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepresentantesAlumnos");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Representantes");
        }
    }
}
