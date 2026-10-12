using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuntalyColegios.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Cierra el acceso de la API REST de Supabase (roles anon/authenticated) a las tablas de la
    /// aplicación: aviso "rls_disabled_in_public". Ver <see cref="ProteccionSupabase"/>.
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
