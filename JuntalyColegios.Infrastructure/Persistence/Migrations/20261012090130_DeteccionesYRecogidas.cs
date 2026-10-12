using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuntalyColegios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeteccionesYRecogidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashClaveAntena",
                table: "Colegios",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Detecciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColegioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetectadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Resultado = table.Column<int>(type: "integer", nullable: false),
                    Simulada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detecciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detecciones_Colegios_ColegioId",
                        column: x => x.ColegioId,
                        principalTable: "Colegios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Detecciones_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Recogidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColegioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeteccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EntregadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recogidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recogidas_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recogidas_Colegios_ColegioId",
                        column: x => x.ColegioId,
                        principalTable: "Colegios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recogidas_Detecciones_DeteccionId",
                        column: x => x.DeteccionId,
                        principalTable: "Detecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Colegios_HashClaveAntena",
                table: "Colegios",
                column: "HashClaveAntena",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detecciones_ColegioId_DetectadoEn",
                table: "Detecciones",
                columns: new[] { "ColegioId", "DetectadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Detecciones_VehiculoId_DetectadoEn",
                table: "Detecciones",
                columns: new[] { "VehiculoId", "DetectadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Recogidas_AlumnoId",
                table: "Recogidas",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recogidas_ColegioId_EntregadoEn",
                table: "Recogidas",
                columns: new[] { "ColegioId", "EntregadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Recogidas_DeteccionId",
                table: "Recogidas",
                column: "DeteccionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recogidas");

            migrationBuilder.DropTable(
                name: "Detecciones");

            migrationBuilder.DropIndex(
                name: "IX_Colegios_HashClaveAntena",
                table: "Colegios");

            migrationBuilder.DropColumn(
                name: "HashClaveAntena",
                table: "Colegios");
        }
    }
}
