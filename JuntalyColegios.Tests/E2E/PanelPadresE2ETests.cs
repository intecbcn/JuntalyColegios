using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Recorrido del padre: ve a su hijo, registra su vehículo y autoriza a la abuela a recogerlo.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class PanelPadresE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public PanelPadresE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_fixture.Factory, "supervisor.panel.e2e@juntaly.test", "Colegio E2E Urdaneta");
        var clase = await DatosDePrueba.CrearClaseAsync(_fixture.Factory, colegio.Id, "1.º A");
        var hijo = await DatosDePrueba.CrearAlumnoAsync(_fixture.Factory, clase, "Tomás", "Lugo");
        await DatosDePrueba.CrearRepresentanteAsync(_fixture.Factory, colegio.Id, "padre.panel.e2e@juntaly.test", hijo);
        _page = await _fixture.NuevaPaginaAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Padre_VeASuHijoRegistraVehiculoYAutorizaAUnaPersona()
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", "padre.panel.e2e@juntaly.test");
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");

        // Tras el login, el panel lleva al padre a su familia.
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Mi familia" })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Row, new() { Name = "Tomás Lugo" })).ToContainTextAsync("1.º A");

        await _page.GetByLabel("Placa").FillAsync("lg 456 tm");
        await _page.GetByLabel("Descripción").FillAsync("Fortuner blanca");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Registrar vehículo" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Row, new() { Name = "LG456TM" })).ToContainTextAsync("Pendiente");

        await _page.GetByLabel("Nombre de la persona").FillAsync("Elena Lugo");
        await _page.GetByLabel("Documento").FillAsync("V-5555555");
        await _page.GetByLabel("Parentesco").FillAsync("Abuela");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Autorizar" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Row, new() { Name = "Elena Lugo" })).ToContainTextAsync("V-5555555");
    }
}
