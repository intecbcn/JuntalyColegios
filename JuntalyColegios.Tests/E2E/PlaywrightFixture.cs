using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace JuntalyColegios.Tests.E2E;

/// <summary>
/// Levanta la app real sobre Kestrel (puerto libre) con los mismos dobles de prueba que los tests de
/// integración y abre un Chromium headless. Si el navegador no está instalado, lo descarga la
/// primera vez (no requiere Node ni PowerShell).
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private readonly KestrelWebApplicationFactory _factory = new();
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    public string BaseUrl { get; private set; } = string.Empty;

    public FakeEmailSender EmailSender => _factory.EmailSender;

    /// <summary>Factory de la app levantada, para sembrar datos de prueba antes de cada recorrido.</summary>
    public JuntalyWebApplicationFactory Factory => _factory;

    public async Task InitializeAsync()
    {
        var codigo = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (codigo != 0)
            throw new InvalidOperationException($"No se pudo instalar Chromium para Playwright (código {codigo}).");

        _factory.UseKestrel();
        _factory.StartServer();
        BaseUrl = _factory.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        _playwright?.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>Página nueva con contexto aislado (cookies propias) apuntando a la app levantada.</summary>
    public async Task<IPage> NuevaPaginaAsync()
    {
        var contexto = await Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl, Locale = "es-VE" });
        return await contexto.NewPageAsync();
    }

    private sealed class KestrelWebApplicationFactory : JuntalyWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseUrls("http://127.0.0.1:0");
            // Los recorridos de demo usan las cuentas de prueba sembradas al arrancar.
            builder.UseSetting("Demo:Sembrar", "true");
            builder.UseSetting("Demo:Contrasena", AutenticacionTests.ContrasenaPorDefecto);
        }
    }
}

[CollectionDefinition(Nombre)]
public sealed class E2ECollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Nombre = "E2E";
}
