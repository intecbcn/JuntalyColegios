using JuntalyColegios.Application.Ports.Out;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>
/// Levanta el pipeline real de la app (Program.cs) para tests de integración, sustituyendo los
/// adaptadores externos: el envío SMTP se reemplaza por <see cref="FakeEmailSender"/> y la cadena de
/// conexión apunta a un PostgreSQL ficticio (los tests no deben tocar la base de datos real).
/// </summary>
public class JuntalyWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ConexionFicticia = "Host=localhost;Port=5432;Database=juntaly_tests;Username=tests;Password=tests";

    public FakeEmailSender EmailSender { get; } = new();

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
        });
    }
}
