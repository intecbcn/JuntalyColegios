using System.Net;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Punto de entrada tras iniciar sesión: /Panel lleva a cada usuario a la zona de su rol (la landing es
/// comercial y no tiene menú de la aplicación).
/// </summary>
public class PanelIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public PanelIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_SinReturnUrl_RedirigeAlPanel()
    {
        await AutenticacionTests.CrearUsuarioAsync(_factory, "panel.login@juntaly.test");
        var cliente = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Identity/Account/Login"));

        var respuesta = await cliente.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "panel.login@juntaly.test",
            ["Input.Password"] = AutenticacionTests.ContrasenaPorDefecto,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Panel", respuesta.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("panel.admin@juntaly.test", Roles.AdminApp, "/Admin")]
    [InlineData("panel.supervisor@juntaly.test", Roles.SupervisorColegio, "/Colegio")]
    [InlineData("panel.padre@juntaly.test", Roles.Padre, "/Padres")]
    public async Task Panel_SegunElRol_RedirigeASuZona(string email, string rol, string destino)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync(email, rol);

        var respuesta = await cliente.GetAsync("/Panel");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(destino, respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Panel_UsuarioSinRol_RedirigeASuCuenta()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("panel.sinrol@juntaly.test");

        var respuesta = await cliente.GetAsync("/Panel");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Identity/Account/Manage", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Panel_SinSesion_RedirigeAlLogin()
    {
        var cliente = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var respuesta = await cliente.GetAsync("/Panel");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Landing_ConSesion_OfreceIrAlPanelEnVezDeIngresar()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("panel.landing@juntaly.test", Roles.Padre);

        var html = await cliente.GetStringAsync("/");

        Assert.Contains("href=\"/Panel\"", html);
        Assert.Contains("Mi panel", html);
    }

    [Fact]
    public async Task Landing_SinSesion_OfreceIngresar()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("Ingresar", html);
        Assert.DoesNotContain("href=\"/Panel\"", html);
    }
}
