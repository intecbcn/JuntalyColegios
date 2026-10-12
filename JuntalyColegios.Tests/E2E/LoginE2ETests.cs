using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>Formulario de inicio de sesión: botón para ver la contraseña escrita.</summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class LoginE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;

    public LoginE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.NuevaPaginaAsync();
        await _page.GotoAsync("/Identity/Account/Login");
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task BotonOjo_MuestraYOcultaLaContrasena()
    {
        var clave = _page.Locator("#Input_Password");
        await clave.FillAsync("Secreta123!");
        await Expect(clave).ToHaveAttributeAsync("type", "password");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Mostrar contraseña" }).ClickAsync();
        await Expect(clave).ToHaveAttributeAsync("type", "text");
        await Expect(clave).ToHaveValueAsync("Secreta123!");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Ocultar contraseña" }).ClickAsync();
        await Expect(clave).ToHaveAttributeAsync("type", "password");
    }
}
