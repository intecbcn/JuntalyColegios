using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JuntalyColegios.Infrastructure.Demo;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Tests.Integration;

/// <summary>App arrancada con los datos de demostración activados (Demo__Sembrar=true).</summary>
public sealed class JuntalyConDemoFactory : JuntalyWebApplicationFactory
{
    public const string ClaveAntenaDemo = "clave-antena-de-la-demo-para-tests-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Demo:Sembrar", "true");
        builder.UseSetting("Demo:Contrasena", AutenticacionTests.ContrasenaPorDefecto);
        builder.UseSetting("Demo:ClaveAntena", ClaveAntenaDemo);
    }
}

/// <summary>
/// Datos de demostración: un colegio completo con un supervisor y un padre de prueba con los que el
/// revisor puede entrar y probar la aplicación (incluida la antena) en cualquier momento.
/// </summary>
public class DemoIntegrationTests : IClassFixture<JuntalyConDemoFactory>
{
    private readonly JuntalyConDemoFactory _factory;

    public DemoIntegrationTests(JuntalyConDemoFactory factory)
    {
        _factory = factory;
        _factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task SupervisorDemo_EntraEnElColegioDeDemoConClasesYFranjas()
    {
        var supervisor = await _factory.IniciarSesionAsync(SembradorDemo.EmailSupervisor, AutenticacionTests.ContrasenaPorDefecto);

        var inicio = await supervisor.GetStringAsync("/Colegio");
        var franjas = await supervisor.GetStringAsync("/Colegio/Franjas");

        Assert.Contains(SembradorDemo.NombreColegio, inicio);
        Assert.Contains("5.º Año", inicio);
        Assert.Contains("00:00 – 23:59", franjas);
        Assert.Contains("13:00 – 14:00", franjas);
    }

    [Fact]
    public async Task PadreDemo_VeASusHijosVehiculoAutorizadaEHistorial()
    {
        var padre = await _factory.IniciarSesionAsync(SembradorDemo.EmailPadre, AutenticacionTests.ContrasenaPorDefecto);

        var html = await padre.GetStringAsync("/Padres");

        Assert.Contains("Sofía Martínez", html);
        Assert.Contains("Diego Martínez", html);
        Assert.Contains("AB123CD", html);
        Assert.Contains("Carmen Rivas", html);
        Assert.Contains("Últimas recogidas", html);
        Assert.Contains("Entregado", html);
    }

    [Fact]
    public async Task AntenaDemo_ConLaClaveConfigurada_LlamaASofiaYNoADiegoFueraDeSuFranja()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones")
        {
            Content = JsonContent.Create(new { tag = SembradorDemo.TagFamiliaDemo })
        };
        peticion.Headers.Add("X-Antena-Clave", JuntalyConDemoFactory.ClaveAntenaDemo);

        var respuesta = await _factory.CreateClient().SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var llamados = json.GetProperty("alumnos").EnumerateArray().Select(a => a.GetString()).ToList();
        Assert.Contains("Sofía Martínez", llamados);
        Assert.DoesNotContain("Diego Martínez", llamados);
    }

    [Fact]
    public async Task Sembrar_DosVeces_NoDuplicaNada()
    {
        int colegios = 0, alumnos = 0, recogidas = 0;
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
        {
            colegios = await db.Colegios.CountAsync();
            alumnos = await db.Alumnos.CountAsync();
            recogidas = await db.Recogidas.CountAsync();
        });

        await SembradorDemo.SembrarAsync(_factory.Services, new OpcionesDemo(AutenticacionTests.ContrasenaPorDefecto, null));

        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
        {
            Assert.Equal(colegios, await db.Colegios.CountAsync());
            Assert.Equal(alumnos, await db.Alumnos.CountAsync());
            Assert.Equal(recogidas, await db.Recogidas.CountAsync());
        });
    }

    [Fact]
    public async Task SinActivarLaDemo_NoSeCreanDatosDeDemo()
    {
        using var sinDemo = new JuntalyWebApplicationFactory();

        await DatosDePrueba.EnBaseDeDatosAsync(sinDemo, async db =>
            Assert.False(await db.Colegios.AnyAsync(c => c.Nombre == SembradorDemo.NombreColegio)));
    }
}
