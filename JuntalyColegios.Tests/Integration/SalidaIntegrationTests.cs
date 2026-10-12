using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Flujo RFID completo sobre el pipeline real: la antena envía la lectura con la clave de su colegio y la
/// pantalla de salida muestra a quién llamar (solo dentro de la franja de retiro).
/// </summary>
public partial class SalidaIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    // Lunes 12/10/2026 16:05 UTC = 12:05 en Caracas, dentro de la franja 12:00–13:00.
    private static readonly DateTimeOffset EnFranja = new(2026, 10, 12, 16, 5, 0, TimeSpan.Zero);

    private readonly JuntalyWebApplicationFactory _factory;

    public SalidaIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reloj.SetUtcNow(EnFranja);
    }

    [GeneratedRegex("<code[^>]*id=\"clave-antena\"[^>]*>([^<]+)</code>")]
    private static partial Regex ClaveMostrada();

    private sealed record Escenario(Colegio Colegio, string Clave, string Tag, string Alumno, string EmailSupervisor);

    /// <summary>Colegio con franja de lunes 12:00–13:00, un padre con una hija y su carro con tag.</summary>
    private async Task<Escenario> EscenarioAsync(string prefijo)
    {
        var emailSupervisor = $"sup.{prefijo}@juntaly.test";
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, emailSupervisor, $"Colegio {prefijo}");
        var clase = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "2.º A");
        var hija = await DatosDePrueba.CrearAlumnoAsync(_factory, clase, "Valeria", $"Salida{prefijo}");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, $"padre.{prefijo}@juntaly.test", hija);
        var tag = $"E200{prefijo.ToUpperInvariant()}01";
        await DatosDePrueba.CrearVehiculoAsync(_factory, padre, $"{prefijo.ToUpperInvariant()[..3]}123", tag);
        await DatosDePrueba.CrearFranjaAsync(_factory, colegio.Id, DayOfWeek.Monday, "12:00", "13:00");
        var clave = await DatosDePrueba.EstablecerClaveAntenaAsync(_factory, colegio.Id);
        return new Escenario(colegio, clave, tag, hija.NombreCompleto, emailSupervisor);
    }

    private async Task<HttpResponseMessage> EnviarLecturaAsync(string? clave, string tag)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones") { Content = JsonContent.Create(new { tag }) };
        if (clave is not null) peticion.Headers.Add("X-Antena-Clave", clave);
        return await _factory.CreateClient().SendAsync(peticion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("clave-incorrecta")]
    public async Task Antena_SinClaveOConClaveIncorrecta_401SinRegistrar(string? clave)
    {
        var escenario = await EscenarioAsync(clave is null ? "sinclave" : "malaclave");

        var respuesta = await EnviarLecturaAsync(clave, escenario.Tag);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.False(await db.Detecciones.AnyAsync(d => d.ColegioId == escenario.Colegio.Id)));
    }

    [Fact]
    public async Task Antena_TagInvalido_400()
    {
        var escenario = await EscenarioAsync("taginvalido");

        var respuesta = await EnviarLecturaAsync(escenario.Clave, "<script>");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Antena_EnFranja_LlamaALaAlumnaYApareceEnLaPantallaDeSalida()
    {
        var escenario = await EscenarioAsync("enfranja");

        var respuesta = await EnviarLecturaAsync(escenario.Clave, escenario.Tag);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("GeneraRecogida", json.GetProperty("resultado").GetString());
        Assert.Equal(escenario.Alumno, json.GetProperty("alumnos")[0].GetString());

        var supervisor = await _factory.IniciarSesionAsync(escenario.EmailSupervisor, AutenticacionTests.ContrasenaPorDefecto);
        Assert.Contains(escenario.Alumno, await supervisor.GetStringAsync("/Colegio/Salida/Pendientes"));
    }

    [Fact]
    public async Task Antena_FueraDeFranja_NoLlamaANadieYLaPantallaNoCambia()
    {
        var escenario = await EscenarioAsync("fuerafranja");
        _factory.Reloj.SetUtcNow(EnFranja.AddHours(8)); // 20:05 en Caracas

        var respuesta = await EnviarLecturaAsync(escenario.Clave, escenario.Tag);

        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("FueraDeFranja", json.GetProperty("resultado").GetString());
        var supervisor = await _factory.IniciarSesionAsync(escenario.EmailSupervisor, AutenticacionTests.ContrasenaPorDefecto);
        Assert.DoesNotContain(escenario.Alumno, await supervisor.GetStringAsync("/Colegio/Salida/Pendientes"));
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.Equal(ResultadoDeteccion.FueraDeFranja, (await db.Detecciones.SingleAsync(d => d.ColegioId == escenario.Colegio.Id)).Resultado));
    }

    [Fact]
    public async Task Supervisor_SimulaLlegadaYMarcaEntregada()
    {
        var escenario = await EscenarioAsync("simular");
        var supervisor = await _factory.IniciarSesionAsync(escenario.EmailSupervisor, AutenticacionTests.ContrasenaPorDefecto);
        Guid vehiculoId = default;
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db => vehiculoId = (await db.Vehiculos.SingleAsync(v => v.Tag == escenario.Tag)).Id);

        var token = AutenticacionTests.ExtraerTokenAntiforgery(await supervisor.GetStringAsync("/Colegio/Salida"));
        await supervisor.PostAsync("/Colegio/Salida/Simular", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["vehiculoId"] = vehiculoId.ToString(),
            ["__RequestVerificationToken"] = token
        }));
        Assert.Contains(escenario.Alumno, await supervisor.GetStringAsync("/Colegio/Salida/Pendientes"));

        Guid recogidaId = default;
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            recogidaId = (await db.Recogidas.SingleAsync(r => r.ColegioId == escenario.Colegio.Id)).Id);
        await supervisor.PostAsync("/Colegio/Salida/Entregar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["recogidaId"] = recogidaId.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.DoesNotContain(escenario.Alumno, await supervisor.GetStringAsync("/Colegio/Salida/Pendientes"));
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.NotNull((await db.Recogidas.SingleAsync(r => r.Id == recogidaId)).EntregadoEn));
    }

    [Fact]
    public async Task Supervisor_EntregarRecogidaDeOtroColegio_404()
    {
        var escenarioA = await EscenarioAsync("colegioa");
        var escenarioB = await EscenarioAsync("colegiob");
        await EnviarLecturaAsync(escenarioB.Clave, escenarioB.Tag);
        Guid recogidaB = default;
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            recogidaB = (await db.Recogidas.SingleAsync(r => r.ColegioId == escenarioB.Colegio.Id)).Id);
        var supervisorA = await _factory.IniciarSesionAsync(escenarioA.EmailSupervisor, AutenticacionTests.ContrasenaPorDefecto);
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await supervisorA.GetStringAsync("/Colegio/Salida"));

        var respuesta = await supervisorA.PostAsync("/Colegio/Salida/Entregar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["recogidaId"] = recogidaB.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain(escenarioB.Alumno, await supervisorA.GetStringAsync("/Colegio/Salida/Pendientes"));
    }

    [Fact]
    public async Task PantallaSalida_Padre_AccesoDenegado()
    {
        var padre = await _factory.CrearClienteAutenticadoAsync("padre.salida@juntaly.test", Roles.Padre);

        var respuesta = await padre.GetAsync("/Colegio/Salida");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Admin_GeneraClaveDeAntena_SeMuestraUnaVezYLaAntenaPuedeUsarla()
    {
        var escenario = await EscenarioAsync("adminclave");
        var admin = await _factory.CrearClienteAutenticadoAsync("admin.clave@juntaly.test", Roles.AdminApp);
        var urlDetalle = $"/Admin/Colegios/Detalle/{escenario.Colegio.Id}";
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await admin.GetStringAsync(urlDetalle));

        await admin.PostAsync($"/Admin/Colegios/GenerarClaveAntena/{escenario.Colegio.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        var primeraVista = await admin.GetStringAsync(urlDetalle);
        var clave = ClaveMostrada().Match(primeraVista).Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(clave), "La clave nueva debe mostrarse tras generarla.");
        Assert.DoesNotMatch(ClaveMostrada(), await admin.GetStringAsync(urlDetalle));

        Assert.Equal(HttpStatusCode.OK, (await EnviarLecturaAsync(clave, escenario.Tag)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarLecturaAsync(escenario.Clave, escenario.Tag)).StatusCode);
    }
}
