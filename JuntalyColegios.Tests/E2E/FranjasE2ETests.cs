using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Recorrido del supervisor: configura la franja de salida de lunes a viernes para todo el colegio.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class FranjasE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public FranjasE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_fixture.Factory, "supervisor.franjas.e2e@juntaly.test", "Colegio E2E Ribas");
        _page = await _fixture.NuevaPaginaAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Supervisor_ConfiguraLaSalidaDeLunesAViernes()
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", "supervisor.franjas.e2e@juntaly.test");
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");

        await _page.GetByRole(AriaRole.Link, new() { Name = "Franjas de retiro" }).ClickAsync();
        await Expect(_page.GetByText("no se llamará a ningún alumno")).ToBeVisibleAsync();

        await _page.GetByLabel("Aplica a").SelectOptionAsync(new SelectOptionValue { Label = "Todo el colegio" });
        foreach (var dia in new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" })
            await _page.GetByLabel(dia, new() { Exact = true }).CheckAsync();
        await _page.GetByLabel("Desde").FillAsync("12:00");
        await _page.GetByLabel("Hasta").FillAsync("13:00");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Añadir franja" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Alert)).ToContainTextAsync("5 franjas");
        await Expect(_page.Locator("table tbody tr")).ToHaveCountAsync(5);
        await Expect(_page.GetByRole(AriaRole.Row, new() { Name = "Miércoles" })).ToContainTextAsync("12:00 – 13:00");
    }
}
