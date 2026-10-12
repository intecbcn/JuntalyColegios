using System.Net;
using System.Net.Http.Json;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace JuntalyColegios.Tests.Integration;

/// <summary>App con un límite bajo de lecturas por minuto para poder superarlo en el test.</summary>
public sealed class JuntalyConLimiteBajoFactory : JuntalyWebApplicationFactory
{
    public const int Limite = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Antena:PeticionesPorMinuto", Limite.ToString());
    }
}

/// <summary>
/// La API de la antena es pública: se limita el número de lecturas por minuto de cada antena (cada
/// clave), para que un cliente abusivo no sature la aplicación ni afecte a otros colegios.
/// </summary>
public class LimiteAntenaIntegrationTests : IClassFixture<JuntalyConLimiteBajoFactory>
{
    private readonly JuntalyConLimiteBajoFactory _factory;

    public LimiteAntenaIntegrationTests(JuntalyConLimiteBajoFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpResponseMessage> LeerAsync(HttpClient cliente, string clave)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones") { Content = JsonContent.Create(new { tag = "E200LIMITE01" }) };
        peticion.Headers.Add("X-Antena-Clave", clave);
        return await cliente.SendAsync(peticion);
    }

    [Fact]
    public async Task Antena_SuperaElLimitePorMinuto_Responde429ConRetryAfter()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.limite@juntaly.test");
        var clave = await DatosDePrueba.EstablecerClaveAntenaAsync(_factory, colegio.Id);
        var cliente = _factory.CreateClient();

        for (var i = 0; i < JuntalyConLimiteBajoFactory.Limite; i++)
            Assert.Equal(HttpStatusCode.OK, (await LeerAsync(cliente, clave)).StatusCode);
        var excedida = await LeerAsync(cliente, clave);

        Assert.Equal(HttpStatusCode.TooManyRequests, excedida.StatusCode);
        Assert.NotNull(excedida.Headers.RetryAfter);
    }

    [Fact]
    public async Task Antena_ElLimiteDeUnColegioNoAfectaAOtro()
    {
        var colegioA = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.limite.a@juntaly.test", "Colegio A");
        var colegioB = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.limite.b@juntaly.test", "Colegio B");
        var claveA = await DatosDePrueba.EstablecerClaveAntenaAsync(_factory, colegioA.Id);
        var claveB = await DatosDePrueba.EstablecerClaveAntenaAsync(_factory, colegioB.Id);
        var cliente = _factory.CreateClient();

        for (var i = 0; i <= JuntalyConLimiteBajoFactory.Limite; i++)
            await LeerAsync(cliente, claveA);

        Assert.Equal(HttpStatusCode.OK, (await LeerAsync(cliente, claveB)).StatusCode);
    }

    [Fact]
    public async Task OtrasPaginas_NoEstanLimitadas()
    {
        var cliente = _factory.CreateClient();

        for (var i = 0; i <= JuntalyConLimiteBajoFactory.Limite * 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/")).StatusCode);
    }
}
