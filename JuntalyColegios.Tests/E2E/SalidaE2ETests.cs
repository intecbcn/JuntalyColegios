using System.Net.Http.Json;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace JuntalyColegios.Tests.E2E;

/// <summary>
/// Pantalla de salida en un navegador real: el simulador llama al alumno, el personal lo entrega y una
/// lectura real de la antena aparece sola gracias a la actualización automática.
/// </summary>
[Collection(E2ECollection.Nombre)]
[Trait("Category", "E2E")]
public class SalidaE2ETests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;
    private string _claveAntena = string.Empty;

    // xUnit crea una instancia por test: cada una siembra su propio colegio y cuentas.
    private readonly string _id = Guid.NewGuid().ToString("N")[..8];

    public SalidaE2ETests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var factory = _fixture.Factory;
        // Lunes 12:05 en Caracas, dentro de la franja.
        factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(factory, $"supervisor.salida.{_id}@juntaly.test", "Colegio E2E Páez");
        var clase = await DatosDePrueba.CrearClaseAsync(factory, colegio.Id, "4.º B");
        await DatosDePrueba.CrearFranjaAsync(factory, colegio.Id, DayOfWeek.Monday, "12:00", "13:00");

        var mateo = await DatosDePrueba.CrearAlumnoAsync(factory, clase, "Mateo", "Salazar");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(factory, colegio.Id, $"padre.salida.{_id}@juntaly.test", mateo);
        await DatosDePrueba.CrearVehiculoAsync(factory, padre, "SAL001", "E200SALIDA01");
        await DatosDePrueba.EnBaseDeDatosAsync(factory, db =>
        {
            db.PersonasAutorizadas.Add(JuntalyColegios.Domain.Entities.PersonaAutorizada.Crear(padre, "Rosa Salazar", "V-7777777", "Abuela"));
            return db.SaveChangesAsync();
        });

        var lucia = await DatosDePrueba.CrearAlumnoAsync(factory, clase, "Lucía", "Antena");
        var otroPadre = await DatosDePrueba.CrearRepresentanteAsync(factory, colegio.Id, $"otro.salida.{_id}@juntaly.test", lucia);
        await DatosDePrueba.CrearVehiculoAsync(factory, otroPadre, "ANT002", "E200SALIDA02");
        _claveAntena = await DatosDePrueba.EstablecerClaveAntenaAsync(factory, colegio.Id);

        _page = await _fixture.NuevaPaginaAsync();
        await _page.GotoAsync("/Identity/Account/Login");
        await _page.FillAsync("#Input_Email", $"supervisor.salida.{_id}@juntaly.test");
        await _page.FillAsync("#Input_Password", AutenticacionTests.ContrasenaPorDefecto);
        await _page.ClickAsync("#login-submit");
        await _page.GetByRole(AriaRole.Link, new() { Name = "Pantalla de salida" }).ClickAsync();
    }

    public async Task DisposeAsync() => await _page.Context.CloseAsync();

    [Fact]
    public async Task Simulador_LlamaAlAlumnoConSusAutorizados_YAlEntregarloDesaparece()
    {
        await _page.GetByLabel("Vehículo").SelectOptionAsync(new SelectOptionValue { Label = "SAL001 · Padre de prueba" });
        await _page.GetByRole(AriaRole.Button, new() { Name = "Simular llegada" }).ClickAsync();

        var tarjeta = _page.Locator(".recogida", new() { HasText = "Mateo Salazar" });
        await Expect(tarjeta).ToBeVisibleAsync();
        await Expect(tarjeta).ToContainTextAsync("4.º B");
        await Expect(tarjeta).ToContainTextAsync("SAL001");
        await Expect(tarjeta).ToContainTextAsync("Rosa Salazar");

        await tarjeta.GetByRole(AriaRole.Button, new() { Name = "Entregado" }).ClickAsync();
        await Expect(_page.Locator(".recogida", new() { HasText = "Mateo Salazar" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task LecturaDeLaAntena_ApareceSolaEnLaPantallaSinRecargar()
    {
        await Expect(_page.Locator(".recogida", new() { HasText = "Lucía Antena" })).ToHaveCountAsync(0);

        using var http = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones") { Content = JsonContent.Create(new { tag = "E200SALIDA02" }) };
        peticion.Headers.Add("X-Antena-Clave", _claveAntena);
        (await http.SendAsync(peticion)).EnsureSuccessStatusCode();

        await Expect(_page.Locator(".recogida", new() { HasText = "Lucía Antena" })).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
