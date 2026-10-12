using System.Net;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Infrastructure.Identity;
using JuntalyColegios.Infrastructure.Persistence;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Roles de la aplicación, administrador inicial, registro público cerrado (el alta es en cascada) y
/// control de acceso al área de administración.
/// </summary>
public class RolesYAccesoIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public RolesYAccesoIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClienteAnonimo() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Arranque_CreaLosTresRolesDeLaAplicacion()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var roles = await db.Roles.Select(r => r.Name).ToListAsync();

        Assert.Contains(Roles.AdminApp, roles);
        Assert.Contains(Roles.SupervisorColegio, roles);
        Assert.Contains(Roles.Padre, roles);
    }

    [Fact]
    public async Task Arranque_CreaElAdministradorInicialConfigurado_YPuedeIniciarSesion()
    {
        _ = _factory.Services; // fuerza el arranque (y la inicialización) de la app

        var cliente = await _factory.IniciarSesionAsync(JuntalyWebApplicationFactory.EmailAdminInicial, AutenticacionTests.ContrasenaPorDefecto);
        var respuesta = await cliente.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task InicializarIdentidad_EjecutadoDosVeces_NoDuplicaRolesNiAdministrador()
    {
        await InicializadorIdentidad.InicializarAsync(_factory.Services);
        await InicializadorIdentidad.InicializarAsync(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Roles.CountAsync(r => r.Name == Roles.AdminApp));
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == JuntalyWebApplicationFactory.EmailAdminInicial));
    }

    [Fact]
    public async Task GetRegistro_RegistroPublicoCerrado_RedirigeAlLogin()
    {
        var respuesta = await ClienteAnonimo().GetAsync("/Identity/Account/Register");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task PostRegistro_RegistroPublicoCerrado_NoCreaElUsuario()
    {
        var cliente = ClienteAnonimo();
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Identity/Account/Login"));

        await cliente.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "intruso@juntaly.test",
            ["Input.Password"] = "Intruso123!",
            ["Input.ConfirmPassword"] = "Intruso123!",
            ["__RequestVerificationToken"] = token
        }));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Email == "intruso@juntaly.test"));
    }

    [Theory]
    [InlineData(JuntalyWebApplicationFactory.EmailAdminInicial)]
    [InlineData("no-existe@juntaly.test")]
    public async Task GetConfirmacionDeRegistro_RegistroCerrado_RedirigeAlLoginSinEnlaceDeConfirmacion(string email)
    {
        _ = _factory.Services;

        var respuesta = await ClienteAnonimo().GetAsync($"/Identity/Account/RegisterConfirmation?email={Uri.EscapeDataString(email)}");

        // Antes devolvía un enlace válido de confirmación para cualquier usuario y un 404 si el email no
        // existía (permitía saber qué emails tienen cuenta).
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task GetLogin_NoOfreceEnlaceDeRegistro()
    {
        var html = await ClienteAnonimo().GetStringAsync("/Identity/Account/Login");

        Assert.DoesNotContain("Account/Register", html);
    }

    [Fact]
    public async Task GetAdmin_SinSesion_RedirigeAlLogin()
    {
        var respuesta = await ClienteAnonimo().GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("sinrol@juntaly.test")]
    [InlineData("supervisor.acceso@juntaly.test", Roles.SupervisorColegio)]
    [InlineData("padre.acceso@juntaly.test", Roles.Padre)]
    public async Task GetAdmin_UsuarioSinRolAdminApp_AccesoDenegado(string email, params string[] roles)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync(email, roles);

        var respuesta = await cliente.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task GetAdmin_UsuarioAdminApp_MuestraElPanelDeAdministracion()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("otro.admin@juntaly.test", Roles.AdminApp);

        var respuesta = await cliente.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Administración", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Menu_UsuarioAdminApp_MuestraEnlaceAAdministracion()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("menu.admin@juntaly.test", Roles.AdminApp);

        var html = await cliente.GetStringAsync("/Home/Privacy");

        Assert.Contains("href=\"/Admin\"", html);
    }

    [Fact]
    public async Task Menu_UsuarioSinRolAdminApp_NoMuestraEnlaceAAdministracion()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("menu.padre@juntaly.test", Roles.Padre);

        var html = await cliente.GetStringAsync("/Home/Privacy");

        Assert.DoesNotContain("href=\"/Admin\"", html);
    }
}
