using System.Text.RegularExpressions;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Los paneles internos (Admin, supervisor y padres) usan el layout con la identidad de Juntaly
/// Colegios y sus tablas van dentro de un contenedor con scroll para no desbordar en móvil.
/// </summary>
public partial class LayoutAppIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public LayoutAppIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [GeneratedRegex("<table", RegexOptions.IgnoreCase)]
    private static partial Regex AperturaDeTabla();

    [GeneratedRegex("<div class=\"table-responsive jc-table-wrap\">\\s*<table", RegexOptions.IgnoreCase)]
    private static partial Regex TablaEnvuelta();

    private static void AssertLayoutJuntaly(string html)
    {
        Assert.Contains("css/app.css", html);
        Assert.Contains("jc-app-body", html);
        Assert.Contains("jc-app-header", html);
        Assert.DoesNotContain("css/site.css", html);
        Assert.DoesNotContain(">Privacy<", html);

        var tablas = AperturaDeTabla().Matches(html).Count;
        Assert.True(tablas > 0, "La página debería tener al menos una tabla.");
        Assert.Equal(tablas, TablaEnvuelta().Matches(html).Count);
    }

    [Fact]
    public async Task PanelAdmin_UsaLayoutJuntalyYTablasResponsive()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.layout.admin@juntaly.test", "Colegio Layout Admin");
        var admin = await _factory.CrearClienteAutenticadoAsync("admin.layout@juntaly.test", Roles.AdminApp);

        AssertLayoutJuntaly(await admin.GetStringAsync("/Admin/Colegios"));
    }

    [Fact]
    public async Task PanelSupervisor_UsaLayoutJuntalyYTablasResponsive()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.layout@juntaly.test", "Colegio Layout");
        await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "3.º C");
        var supervisor = await _factory.IniciarSesionAsync("sup.layout@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);

        AssertLayoutJuntaly(await supervisor.GetStringAsync("/Colegio"));
    }

    [Fact]
    public async Task PanelPadres_UsaLayoutJuntalyYTablasResponsive()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.layout.padres@juntaly.test", "Colegio Layout Padres");
        var clase = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "4.º A");
        var hijo = await DatosDePrueba.CrearAlumnoAsync(_factory, clase, "Ana", "Layout");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.layout@juntaly.test", hijo);
        await DatosDePrueba.CrearVehiculoAsync(_factory, padre, "LAY123");
        var cliente = await _factory.IniciarSesionAsync("padre.layout@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);

        AssertLayoutJuntaly(await cliente.GetStringAsync("/Padres"));
    }
}
