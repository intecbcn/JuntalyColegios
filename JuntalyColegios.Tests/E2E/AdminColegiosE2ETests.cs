using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Recorrido del administrador: inicia sesión, crea un colegio y da de alta a su supervisor.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class AdminColegiosE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public AdminColegiosE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _fixture.EmailSender.Reiniciar();
        _page = await _fixture.NuevaPaginaAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Admin_CreaColegioYDaDeAltaSupervisor_LosVeEnPantalla()
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", JuntalyWebApplicationFactory.EmailAdminInicial);
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");

        await _page.GetByRole(AriaRole.Link, new() { Name = "Administración" }).ClickAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Colegios", Exact = true }).ClickAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Nuevo colegio" }).ClickAsync();

        await _page.GetByLabel("Nombre").FillAsync("Colegio E2E Los Próceres");
        await _page.GetByLabel("Dirección").FillAsync("Av. Bolívar 10");
        await _page.GetByLabel("Zona horaria").SelectOptionAsync("America/Caracas");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Colegio E2E Los Próceres" })).ToBeVisibleAsync();

        await _page.GetByLabel("Nombre del supervisor").FillAsync("María Rodríguez");
        await _page.GetByLabel("Email del supervisor").FillAsync("maria.e2e@juntaly.test");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Dar de alta supervisor" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Alert)).ToContainTextAsync("email");
        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "maria.e2e@juntaly.test" })).ToBeVisibleAsync();
        Assert.Contains(_fixture.EmailSender.Enviados, c => c.Destinatario == "maria.e2e@juntaly.test");

        await _page.GotoAsync("/Admin/Colegios");
        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "Colegio E2E Los Próceres" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_EditaYEliminaUnSupervisor()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_fixture.Factory, "sup.gestion.e2e@juntaly.test", "Colegio E2E Miranda");
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", JuntalyWebApplicationFactory.EmailAdminInicial);
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");
        await _page.GotoAsync($"/Admin/Colegios/Detalle/{colegio.Id}");

        var fila = _page.GetByRole(AriaRole.Row, new() { Name = "sup.gestion.e2e@juntaly.test" });
        await fila.GetByRole(AriaRole.Link, new() { Name = "Editar" }).ClickAsync();
        await _page.GetByLabel("Nombre del supervisor").FillAsync("Supervisora Editada");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "Supervisora Editada" })).ToBeVisibleAsync();

        _page.Dialog += (_, dialogo) => dialogo.AcceptAsync();
        await _page.GetByRole(AriaRole.Row, new() { Name = "Supervisora Editada" }).GetByRole(AriaRole.Button, new() { Name = "Eliminar" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Alert)).ToContainTextAsync("eliminado");
        await Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "Supervisora Editada" })).ToHaveCountAsync(0);
    }
}
