using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Recorrido del supervisor: da de alta a un padre con su hija, registra su carro y le asigna el tag RFID.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class SupervisorPadresE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public SupervisorPadresE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_fixture.Factory, "supervisor.padres.e2e@juntaly.test", "Colegio E2E Sucre");
        var clase = await DatosDePrueba.CrearClaseAsync(_fixture.Factory, colegio.Id, "3.º C");
        await DatosDePrueba.CrearAlumnoAsync(_fixture.Factory, clase, "Camila", "Herrera");
        _page = await _fixture.NuevaPaginaAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Supervisor_DaDeAltaPadreConHijaVehiculoYTag()
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", "supervisor.padres.e2e@juntaly.test");
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");

        await _page.GetByRole(AriaRole.Link, new() { Name = "Padres y representantes" }).ClickAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Nuevo padre o representante" }).ClickAsync();

        await _page.GetByLabel("Nombre completo").FillAsync("Ricardo Herrera");
        await _page.GetByLabel("Email").FillAsync("ricardo.e2e@juntaly.test");
        await _page.GetByLabel("Camila Herrera").CheckAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Dar de alta" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Ricardo Herrera" })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "Camila Herrera" })).ToBeVisibleAsync();

        await _page.GetByLabel("Placa").FillAsync("ab123cd");
        await _page.GetByLabel("Descripción").FillAsync("Corolla gris");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Añadir vehículo" }).ClickAsync();

        await _page.GetByLabel("Tag RFID de AB123CD").FillAsync("E2004700A1B2C3D4");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Asignar tag" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Tag asignado");
        await Expect(_page.GetByLabel("Tag RFID de AB123CD")).ToHaveValueAsync("E2004700A1B2C3D4");
        Assert.Contains(_fixture.EmailSender.Enviados, c => c.Destinatario == "ricardo.e2e@juntaly.test");
    }
}
