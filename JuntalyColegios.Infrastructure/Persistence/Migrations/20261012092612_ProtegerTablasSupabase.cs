using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuntalyColegios.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Cierra el acceso de la API REST del proveedor de PostgreSQL (roles anon/authenticated) a las tablas
    /// de la aplicación. Ver <see cref="ProteccionTablasPublicas"/>.
    /// </summary>
    public partial class ProtegerTablasSupabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.ProtegerTablasPublicas();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intencionadamente vacío: revertir esta migración no debe volver a exponer los datos.
        }
    }
}
