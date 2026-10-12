using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuntalyColegios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonasAutorizadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonasAutorizadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepresentanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColegioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Parentesco = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonasAutorizadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonasAutorizadas_Colegios_ColegioId",
                        column: x => x.ColegioId,
                        principalTable: "Colegios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonasAutorizadas_Representantes_RepresentanteId",
                        column: x => x.RepresentanteId,
                        principalTable: "Representantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonasAutorizadas_ColegioId",
                table: "PersonasAutorizadas",
                column: "ColegioId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonasAutorizadas_RepresentanteId_Documento",
                table: "PersonasAutorizadas",
                columns: new[] { "RepresentanteId", "Documento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonasAutorizadas");
        }
    }
}
