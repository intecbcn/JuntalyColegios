using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>
/// Levanta el pipeline real de la app (Program.cs) para tests de integración, sustituyendo los
/// adaptadores externos: el envío SMTP se reemplaza por <see cref="FakeEmailSender"/> y PostgreSQL por
/// una base SQLite en memoria propia de cada factory (los tests nunca tocan la base de datos real).
/// </summary>
public class JuntalyWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ConexionFicticia = "Host=localhost;Port=5432;Database=juntaly_tests;Username=tests;Password=tests";

    // SQLite en memoria vive mientras la conexión siga abierta: se abre una por factory y se cierra al
    // desecharla, así todos los DbContext de la app comparten la misma base durante los tests.
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public FakeEmailSender EmailSender { get; } = new();

    /// <summary>
    /// Reloj de la app en los tests: lunes 12/10/2026 a las 16:05 UTC (12:05 en Caracas). Los tests de
    /// franjas y detecciones lo mueven con Advance/SetUtcNow.
    /// </summary>
    public RelojDePrueba Reloj { get; } = new(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));

    public const string EmailAdminInicial = "admin@juntaly.test";

    /// <summary>URL pública con la que la app construye los enlaces de los emails (p.ej. invitaciones).</summary>
    public const string UrlBase = "https://colegios.test";

    public JuntalyWebApplicationFactory()
    {
        // El esquema se crea antes de arrancar la app, porque Program.cs inicializa roles y el
        // administrador durante el arranque.
        _conexion.Open();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conexion).Options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Fuera de Development los static web assets (wwwroot) no se activan solos y los .js/.css
        // llegarían vacíos: se habilitan para que la app de test sirva lo mismo que en local.
        builder.UseStaticWebAssets();
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConexionFicticia);
        builder.UseSetting("App:UrlBase", UrlBase);
        builder.UseSetting("AdminInicial:Email", EmailAdminInicial);
        builder.UseSetting("AdminInicial:Password", AutenticacionTests.ContrasenaPorDefecto);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Reloj);

            // Desde EF Core 9 AddDbContext registra también la configuración del proveedor por
            // separado: hay que quitar ambas para que no convivan Npgsql y SQLite.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_conexion));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexion.Dispose();
    }
}
