using System.Net;
using JuntalyColegios.Infrastructure.Persistence;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Comprueba la infraestructura de los tests de integración: base de datos en memoria (nunca la real)
/// y la utilidad de inicio de sesión sobre el pipeline real de Identity.
/// </summary>
public class EntornoDeTestsIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public EntornoDeTestsIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Factory_UsaSqliteEnMemoria_NoLaBaseDeDatosReal()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
    }

    [Fact]
    public async Task CrearClienteAutenticado_IniciaSesion_PuedeAbrirLaGestionDeSuCuenta()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("padre.prueba@juntaly.test");

        var respuesta = await cliente.GetAsync("/Identity/Account/Manage");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("padre.prueba@juntaly.test", await respuesta.Content.ReadAsStringAsync());
    }
}
