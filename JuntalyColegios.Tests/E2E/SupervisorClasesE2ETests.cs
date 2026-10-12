using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Recorrido del supervisor: inicia sesión, crea una clase y matricula un alumno.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class SupervisorClasesE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public SupervisorClasesE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_fixture.Factory, "supervisor.e2e@juntaly.test", "Colegio E2E San Martín");
        _page = await _fixture.NuevaPaginaAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Supervisor_CreaClaseYMatriculaAlumno_LoVeEnLaClase()
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", "supervisor.e2e@juntaly.test");
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");

        // Tras el login, el panel lleva al supervisor a su colegio.
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Colegio E2E San Martín" })).ToBeVisibleAsync();

        await _page.GetByRole(AriaRole.Link, new() { Name = "Nueva clase" }).ClickAsync();
        await _page.GetByLabel("Nombre de la clase").FillAsync("2.º B");
        await _page.GetByLabel("Nivel").FillAsync("Primaria");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "2.º B" })).ToBeVisibleAsync();

        await _page.GetByLabel("Nombres").FillAsync("Mateo");
        await _page.GetByLabel("Apellidos").FillAsync("Rivas Ortega");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Añadir alumno" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "Rivas Ortega" })).ToBeVisibleAsync();

        await _page.GetByRole(AriaRole.Link, new() { Name = "Mi colegio" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Row, new() { Name = "2.º B" })).ToContainTextAsync("1");
    }
}
