using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Pantallas de llamada (portería y aulas) sobre el pipeline real: se abren con un enlace secreto sin
/// iniciar sesión y muestran solo lo imprescindible de los alumnos llamados.
/// </summary>
public partial class PantallasIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public PantallasIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));
    }

    [GeneratedRegex("https://colegios\\.test/Pantalla/([A-Za-z0-9_-]{40,})")]
    private static partial Regex EnlacePantalla();

    private HttpClient Anonimo() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Colegio con franja todo el lunes, dos clases y una familia con un hijo en cada una y un carro con tag.</summary>
    private async Task<(JuntalyColegios.Domain.Entities.Colegio Colegio, JuntalyColegios.Domain.Entities.Clase Primero, string ClaveAntena)> EscenarioAsync(string prefijo)
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, $"sup.{prefijo}@juntaly.test", $"Colegio {prefijo}");
        var primero = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "1.º A");
        var quinto = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "5.º Año");
        var hija = await DatosDePrueba.CrearAlumnoAsync(_factory, primero, "Sofía", $"P{prefijo}");
        var hijo = await DatosDePrueba.CrearAlumnoAsync(_factory, quinto, "Diego", $"P{prefijo}");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, $"padre.{prefijo}@juntaly.test", hija, hijo);
        await DatosDePrueba.CrearVehiculoAsync(_factory, padre, "PAN123", $"E200{prefijo.ToUpperInvariant()}");
        await DatosDePrueba.CrearFranjaAsync(_factory, colegio.Id, DayOfWeek.Monday, "00:00", "23:59");
        return (colegio, primero, await DatosDePrueba.EstablecerClaveAntenaAsync(_factory, colegio.Id));
    }

    private async Task LlegaElCarroAsync(string claveAntena, string prefijo)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones")
        {
            Content = JsonContent.Create(new { tag = $"E200{prefijo.ToUpperInvariant()}" })
        };
        peticion.Headers.Add("X-Antena-Clave", claveAntena);
        (await _factory.CreateClient().SendAsync(peticion)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Supervisor_CreaLaPantallaDePorteria_YVeElEnlaceUnaSolaVez()
    {
        await EscenarioAsync("crearpantalla");
        var supervisor = await _factory.IniciarSesionAsync("sup.crearpantalla@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await supervisor.GetStringAsync("/Colegio/Pantallas"));

        var respuesta = await supervisor.PostAsync("/Colegio/Pantallas/Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["claseId"] = "",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var primeraVista = await supervisor.GetStringAsync("/Colegio/Pantallas");
        var enlace = EnlacePantalla().Match(primeraVista);
        Assert.True(enlace.Success, "Tras crearla debe mostrarse el enlace de la pantalla.");
        Assert.Contains("Portería", primeraVista);
        Assert.DoesNotMatch(EnlacePantalla(), await supervisor.GetStringAsync("/Colegio/Pantallas"));
        Assert.Equal(HttpStatusCode.OK, (await Anonimo().GetAsync($"/Pantalla/{enlace.Groups[1].Value}")).StatusCode);
    }

    [Fact]
    public async Task PantallaPorteria_SinSesion_MuestraElLlamadoSinDatosDeLaFamiliaYNoSeIndexa()
    {
        var (colegio, _, clave) = await EscenarioAsync("porteria");
        var token = await DatosDePrueba.CrearPantallaAsync(_factory, colegio.Id);
        await LlegaElCarroAsync(clave, "porteria");

        var respuesta = await Anonimo().GetAsync($"/Pantalla/{token}");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Colegio porteria", html);
        Assert.Contains("Llamando ahora", html);
        Assert.Contains("Sofía Pporteria", html);
        Assert.Contains("Diego Pporteria", html);
        Assert.Contains("PAN123", html);
        Assert.DoesNotContain("Padre de prueba", html);
        Assert.Contains("noindex", respuesta.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Equal("no-referrer", respuesta.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task PantallaDeAula_SoloLosAlumnosDeSuClase()
    {
        var (colegio, primero, clave) = await EscenarioAsync("aula");
        var token = await DatosDePrueba.CrearPantallaAsync(_factory, colegio.Id, primero.Id);
        await LlegaElCarroAsync(clave, "aula");

        var html = await Anonimo().GetStringAsync($"/Pantalla/{token}/Contenido");

        Assert.Contains("Sofía Paula", html);
        Assert.DoesNotContain("Diego Paula", html);
    }

    [Theory]
    [InlineData("token-inventado-que-no-existe-en-ninguna-pantalla-0000")]
    [InlineData("x")]
    public async Task PantallaConEnlaceInvalido_404(string token)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Anonimo().GetAsync($"/Pantalla/{token}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonimo().GetAsync($"/Pantalla/{token}/Contenido")).StatusCode);
    }

    [Fact]
    public async Task Supervisor_RevocaLaPantalla_YSuEnlaceDevuelve404()
    {
        var (colegio, _, _) = await EscenarioAsync("revocar");
        var token = await DatosDePrueba.CrearPantallaAsync(_factory, colegio.Id);
        var supervisor = await _factory.IniciarSesionAsync("sup.revocar@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);
        var pagina = await supervisor.GetStringAsync("/Colegio/Pantallas");
        var id = Regex.Match(pagina, "/Colegio/Pantallas/Revocar/([0-9a-fA-F-]{36})").Groups[1].Value;

        await supervisor.PostAsync($"/Colegio/Pantallas/Revocar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacionTests.ExtraerTokenAntiforgery(pagina)
        }));

        Assert.Equal(HttpStatusCode.NotFound, (await Anonimo().GetAsync($"/Pantalla/{token}")).StatusCode);
    }

    [Fact]
    public async Task GestionDePantallas_Padre_AccesoDenegado()
    {
        var padre = await _factory.CrearClienteAutenticadoAsync("padre.pantallas@juntaly.test", Roles.Padre);

        var respuesta = await padre.GetAsync("/Colegio/Pantallas");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }
}
