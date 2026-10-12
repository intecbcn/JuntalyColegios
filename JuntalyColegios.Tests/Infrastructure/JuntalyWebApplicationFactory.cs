using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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

    public JuntalyWebApplicationFactory()
    {
        _conexion.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Fuera de Development los static web assets (wwwroot) no se activan solos y los .js/.css
        // llegarían vacíos: se habilitan para que la app de test sirva lo mismo que en local.
        builder.UseStaticWebAssets();
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConexionFicticia);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);

            // Desde EF Core 9 AddDbContext registra también la configuración del proveedor por
            // separado: hay que quitar ambas para que no convivan Npgsql y SQLite.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_conexion));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexion.Dispose();
    }
}
