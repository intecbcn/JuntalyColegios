using Microsoft.EntityFrameworkCore.Migrations;

namespace JuntalyColegios.Infrastructure.Persistence.Migrations;

/// <summary>
/// Algunos servicios de PostgreSQL gestionado publican el esquema <c>public</c> por una
/// API REST con los roles <c>anon</c> y <c>authenticated</c>: sin protección, cualquiera con la clave pública
/// podría leer o modificar las tablas (incluidos usuarios y datos de menores). Esta aplicación no usa esa
/// API: accede como dueño de las tablas y por tanto no está sujeta a RLS.
/// </summary>
public static class ProteccionTablasPublicas
{
    /// <summary>
    /// Activa RLS (sin políticas: los roles de la API no ven filas) en todas las tablas de <c>public</c>
    /// y retira a <c>anon</c>/<c>authenticated</c> los permisos actuales y los que se darían por defecto
    /// a tablas futuras. Llamarlo al final de cada migración que cree tablas nuevas. En un PostgreSQL
    /// sin esos roles solo activa RLS.
    /// </summary>
    public static void ProtegerTablasPublicas(this MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            DO $$
            DECLARE t record;
            BEGIN
                FOR t IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
                    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t.tablename);
                END LOOP;

                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon')
                   AND EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                    REVOKE ALL ON ALL TABLES IN SCHEMA public FROM anon, authenticated;
                    REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon, authenticated;
                    REVOKE ALL ON ALL FUNCTIONS IN SCHEMA public FROM anon, authenticated;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON TABLES FROM anon, authenticated;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON SEQUENCES FROM anon, authenticated;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON FUNCTIONS FROM anon, authenticated;
                END IF;
            END $$;
            """);
}
