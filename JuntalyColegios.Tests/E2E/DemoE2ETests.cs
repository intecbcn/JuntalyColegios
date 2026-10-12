using JuntalyColegios.Infrastructure.Demo;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Lo que verá el revisor con las cuentas de prueba: el padre de demo y su historial, y el registro del supervisor.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class DemoE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public DemoE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _fixture.Factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));
        _page = await _fixture.NuevaPaginaAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    private async Task EntrarComoAsync(string email)
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", email);
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");
    }

    [Fact]
    public async Task PadreDemo_VeSuFamiliaYElHistorialDeRecogidas()
    {
        await EntrarComoAsync(SembradorDemo.EmailPadre);

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Mi familia" })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Row, new() { Name = "Sofía Martínez" }).First).ToContainTextAsync("1.º A");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Últimas recogidas" })).ToBeVisibleAsync();
        await Expect(_page.Locator("#historial tbody tr")).Not.ToHaveCountAsync(0);
    }

    [Fact]
    public async Task SupervisorDemo_ConsultaElRegistroDeDetecciones()
    {
        await EntrarComoAsync(SembradorDemo.EmailSupervisor);

        await _page.GetByRole(AriaRole.Link, new() { Name = "Registro de detecciones" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Registro de detecciones" })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "Fuera de franja" }).First).ToBeVisibleAsync();
    }
}
