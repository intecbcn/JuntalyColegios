using System.Net;
using System.Text.RegularExpressions;
using JuntalyColegios.Tests.Infrastructure;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Tests de integración del pipeline HTTP real (rutas, vistas, antiforgery, DI) con
/// <see cref="JuntalyWebApplicationFactory"/>, sin navegador ni servicios externos.
/// </summary>
public partial class LandingIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LandingIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EmailSender.Reiniciar();
        _client = factory.CreateClient();
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenAntiforgery();

    private async Task<string> ObtenerTokenAntiforgeryAsync()
    {
        var html = await _client.GetStringAsync("/");
        var match = TokenAntiforgery().Match(html);
        Assert.True(match.Success, "La landing debe incluir el token antiforgery en el formulario de demo.");
        return match.Groups[1].Value;
    }

    private static FormUrlEncodedContent Formulario(string? token, string? honeypot = null)
    {
        var campos = new Dictionary<string, string>
        {
            ["Nombre"] = "Ana Pérez",
            ["Colegio"] = "Colegio San José",
            ["Contacto"] = "ana@colegio.com",
            ["NumeroAlumnos"] = "350"
        };
        if (token is not null) campos["__RequestVerificationToken"] = token;
        if (honeypot is not null) campos["Empresa2"] = honeypot;
        return new FormUrlEncodedContent(campos);
    }

    [Fact]
    public async Task GetInicio_Devuelve200ConLaLandingDeJuntalyColegios()
    {
        var respuesta = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("Juntaly Colegios", html);
        Assert.Contains("Solicita una demo", html);
    }

    [Theory]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    [InlineData("/Identity/Account/ForgotPassword")]
    public async Task GetPaginasPublicasDeIdentity_Devuelven200(string ruta)
    {
        var respuesta = await _client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task GetRutaInexistente_Devuelve404()
    {
        var respuesta = await _client.GetAsync("/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostSolicitarDemo_ConTokenValido_EnviaElCorreoAVentas()
    {
        var token = await ObtenerTokenAntiforgeryAsync();

        var respuesta = await _client.PostAsync("/Home/SolicitarDemo", Formulario(token));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"ok\":true", await respuesta.Content.ReadAsStringAsync());
        var correo = Assert.Single(_factory.EmailSender.Enviados);
        Assert.Contains("Colegio San José", correo.CuerpoHtml);
    }

    [Fact]
    public async Task PostSolicitarDemo_SinTokenAntiforgery_Devuelve400YNoEnviaCorreo()
    {
        var respuesta = await _client.PostAsync("/Home/SolicitarDemo", Formulario(token: null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(_factory.EmailSender.Enviados);
    }

    [Fact]
    public async Task PostSolicitarDemo_HoneypotRelleno_RespondeOkPeroNoEnviaCorreo()
    {
        var token = await ObtenerTokenAntiforgeryAsync();

        var respuesta = await _client.PostAsync("/Home/SolicitarDemo", Formulario(token, honeypot: "Bot S.A."));

        Assert.Contains("\"ok\":true", await respuesta.Content.ReadAsStringAsync());
        Assert.Empty(_factory.EmailSender.Enviados);
    }
}
