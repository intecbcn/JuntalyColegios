using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuntalyColegios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PantallasLlamadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PantallasLlamadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColegioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    HashToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PantallasLlamadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PantallasLlamadas_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PantallasLlamadas_Colegios_ColegioId",
                        column: x => x.ColegioId,
                        principalTable: "Colegios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PantallasLlamadas_ClaseId",
                table: "PantallasLlamadas",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PantallasLlamadas_ColegioId_ClaseId",
                table: "PantallasLlamadas",
                columns: new[] { "ColegioId", "ClaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PantallasLlamadas_HashToken",
                table: "PantallasLlamadas",
                column: "HashToken",
                unique: true);

            migrationBuilder.ProtegerTablasPublicas();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PantallasLlamadas");
        }
    }
}
