using System.Net.Http.Json;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>
/// Pantalla de llamadas estilo turnero en TV, tablet y móvil: el llamado aparece solo, en grande, y la
/// página no se desborda en horizontal.
/// </summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class PantallasE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private readonly string _id = Guid.NewGuid().ToString("N")[..8];
    private string _clave = string.Empty;
    private string _token = string.Empty;
    private string _tag = string.Empty;

    public PantallasE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var factory = _fixture.Factory;
        factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(factory, $"sup.pantalla.{_id}@juntaly.test", "Colegio E2E Turnero");
        var clase = await DatosDePrueba.CrearClaseAsync(factory, colegio.Id, "2.º B");
        var alumno = await DatosDePrueba.CrearAlumnoAsync(factory, clase, "Valentina", "Turnero Larguísimo Apellido");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(factory, colegio.Id, $"padre.pantalla.{_id}@juntaly.test", alumno);
        _tag = $"E200TURNO{_id.ToUpperInvariant()}";
        await DatosDePrueba.CrearVehiculoAsync(factory, padre, "TUR001", _tag);
        await DatosDePrueba.CrearFranjaAsync(factory, colegio.Id, DayOfWeek.Monday, "00:00", "23:59");
        _clave = await DatosDePrueba.EstablecerClaveAntenaAsync(factory, colegio.Id);
        _token = await DatosDePrueba.CrearPantallaAsync(factory, colegio.Id);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task LlegaElCarroAsync()
    {
        using var http = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones") { Content = JsonContent.Create(new { tag = _tag }) };
        peticion.Headers.Add("X-Antena-Clave", _clave);
        (await http.SendAsync(peticion)).EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(1920, 1080)] // TV de portería
    [InlineData(820, 1180)]  // tablet en vertical
    [InlineData(390, 844)]   // móvil
    public async Task Pantalla_ElLlamadoApareceSoloEnGrandeYSinDesbordarse(int ancho, int alto)
    {
        var page = await _fixture.NuevaPaginaAsync(new ViewportSize { Width = ancho, Height = alto });
        await page.GotoAsync($"/Pantalla/{_token}");
        await Expect(page.GetByText("Sin llamados")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Pantalla completa" })).ToBeVisibleAsync();

        await LlegaElCarroAsync();

        var llamandoAhora = page.Locator(".jc-turno-actual");
        await Expect(llamandoAhora).ToContainTextAsync("Valentina Turnero Larguísimo Apellido", new() { Timeout = 10_000 });
        await Expect(llamandoAhora).ToContainTextAsync("2.º B");
        await Expect(llamandoAhora).ToContainTextAsync("TUR001");
        var desborda = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(desborda, $"La pantalla no debe tener scroll horizontal a {ancho}x{alto}.");
        await page.Context.CloseAsync();
    }
}
