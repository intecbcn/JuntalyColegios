using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Los paneles de supervisor y padres se usan desde el móvil: sin scroll horizontal y con menú plegable.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class LayoutResponsiveE2ETests : IAsyncLifetime
{
    private static readonly ViewportSize Movil = new() { Width = 390, Height = 844 };

    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;
    private Guid _representanteId;

    // xUnit ejecuta InitializeAsync antes de cada test: emails únicos para no chocar con la siembra anterior.
    private readonly string _sufijo = Guid.NewGuid().ToString("N")[..8];
    private string EmailSupervisor => $"supervisor.movil.{_sufijo}@juntaly.test";
    private string EmailPadre => $"padre.movil.{_sufijo}@juntaly.test";

    public LayoutResponsiveE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_fixture.Factory, EmailSupervisor, "Colegio E2E Responsive con nombre largo");
        var clase = await DatosDePrueba.CrearClaseAsync(_fixture.Factory, colegio.Id, "6.º B");
        var hijo = await DatosDePrueba.CrearAlumnoAsync(_fixture.Factory, clase, "Valentina Alejandra", "Rodríguez Montenegro");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_fixture.Factory, colegio.Id, EmailPadre, hijo);
        await DatosDePrueba.CrearVehiculoAsync(_fixture.Factory, padre, $"M{_sufijo[..5]}", $"E200{_sufijo}");
        _representanteId = padre.Id;
        _page = await _fixture.NuevaPaginaAsync(Movil);
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    private async Task IniciarSesionAsync(string email)
    {
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", email);
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");
    }

    private async Task AssertSinScrollHorizontalAsync(string url)
    {
        await _page.GotoAsync(url);
        var desborde = await _page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        Assert.True(desborde <= 0, $"{url} desborda {desborde}px a lo ancho en móvil.");
    }

    [Fact]
    public async Task Padre_EnMovil_SinScrollHorizontalYMenuPlegable()
    {
        await IniciarSesionAsync(EmailPadre);
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Mi familia" })).ToBeVisibleAsync();

        await AssertSinScrollHorizontalAsync("/Padres");

        var enlace = _page.GetByRole(AriaRole.Link, new() { Name = "Mi familia" });
        await Expect(enlace).ToBeHiddenAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Abrir menú" }).ClickAsync();
        await Expect(enlace).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Supervisor_EnMovil_SinScrollHorizontal()
    {
        await IniciarSesionAsync(EmailSupervisor);
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Colegio E2E Responsive con nombre largo" })).ToBeVisibleAsync();

        await AssertSinScrollHorizontalAsync("/Colegio");
        await AssertSinScrollHorizontalAsync("/Colegio/Salida");
        await AssertSinScrollHorizontalAsync("/Colegio/Representantes");
        await AssertSinScrollHorizontalAsync($"/Colegio/Representantes/Detalle/{_representanteId}");
        await AssertSinScrollHorizontalAsync("/Colegio/Franjas");
        await AssertSinScrollHorizontalAsync("/Colegio/Detecciones");
    }
}
