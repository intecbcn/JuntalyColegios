using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Las claves de Data Protection (cookies de sesión, antiforgery, TempData y tokens de los enlaces de
/// invitación) deben sobrevivir a reinicios y reciclajes del app pool de IIS: se guardan en la base.
/// </summary>
public class DataProtectionIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public DataProtectionIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ClavesDeDataProtection_SeGuardanEnLaBaseDeDatos()
    {
        var protector = _factory.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("prueba");
        protector.Protect("dato");

        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.True(await db.DataProtectionKeys.AnyAsync()));
    }
}
