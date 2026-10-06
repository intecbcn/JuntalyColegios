using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Recorridos de la landing en un navegador real (Chromium headless).</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class LandingE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public LandingE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _fixture.EmailSender.Reiniciar();
        _page = await _fixture.NuevaPaginaAsync();
        await _page.GotoAsync("/");
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Landing_Carga_MuestraTituloYFormularioDeDemo()
    {
        await Expect(_page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Juntaly Colegios"));
        await Expect(_page.Locator("#jcForm")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Landing_Carga_SinErroresDeJavaScriptNiRecursosRotos()
    {
        var page = await _fixture.NuevaPaginaAsync();
        var errores = new List<string>();
        page.Console += (_, mensaje) => { if (mensaje.Type == "error") errores.Add(mensaje.Text); };
        page.PageError += (_, error) => errores.Add(error);
        page.Response += (_, respuesta) => { if (respuesta.Status >= 400) errores.Add($"{respuesta.Status} {respuesta.Url}"); };

        await page.GotoAsync("/", new() { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.Empty(errores);
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task DemoInteractiva_PulsarTabletDocente_MuestraSoloEsePanel()
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "Tablet docente" }).ClickAsync();

        await Expect(_page.Locator("#jcPanel-tablet")).ToBeVisibleAsync();
        await Expect(_page.Locator("#jcPanel-pantalla")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Precio_MoverElSlider_ActualizaElTotalMensual()
    {
        await _page.Locator("#jcStudents").FillAsync("1000");

        await Expect(_page.Locator("#jcTotal")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(@"^\$1[.,]?000$"));
    }

    [Fact]
    public async Task SolicitarDemo_FormularioCompleto_MuestraGraciasYEnviaElCorreo()
    {
        var alertas = new List<string>();
        _page.Dialog += async (_, dialogo) => { alertas.Add(dialogo.Message); await dialogo.DismissAsync(); };

        await _page.Locator("input[name=Nombre]").FillAsync("Ana Pérez");
        await _page.Locator("input[name=Colegio]").FillAsync("Colegio San José");
        await _page.Locator("input[name=Contacto]").FillAsync("ana@colegio.com");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Enviar solicitud" }).ClickAsync();

        await Expect(_page.GetByText("¡Gracias!")).ToBeVisibleAsync();
        await Expect(_page.Locator("#jcForm")).ToBeHiddenAsync();
        Assert.Empty(alertas);
        Assert.Contains("Colegio San José", Assert.Single(_fixture.EmailSender.Enviados).CuerpoHtml);
    }

    [Fact]
    public async Task Navegacion_PulsarIngresar_LlevaAlLogin()
    {
        await _page.GetByRole(AriaRole.Link, new() { Name = "Ingresar" }).First.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Identity/Account/Login"));
    }
}
