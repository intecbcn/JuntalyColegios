using System.Net.Http.Json;
using JuntalyColegios.Tests.Infrastructure;

namespace JuntalyColegios.Tests.Integration;

/// <summary>Historial de recogidas del padre y registro de detecciones del supervisor sobre el pipeline real.</summary>
public class HistorialIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    // Lunes 12/10/2026 16:05 UTC = 12:05 en Caracas, dentro de la franja 12:00–13:00.
    private static readonly DateTimeOffset EnFranja = new(2026, 10, 12, 16, 5, 0, TimeSpan.Zero);

    private readonly JuntalyWebApplicationFactory _factory;

    public HistorialIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reloj.SetUtcNow(EnFranja);
    }

    private async Task<(string Clave, string Tag)> ColegioConFamiliaAsync(string prefijo)
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, $"sup.{prefijo}@juntaly.test", $"Colegio {prefijo}");
        var clase = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "6.º A");
        var hijo = await DatosDePrueba.CrearAlumnoAsync(_factory, clase, "Gabriel", $"Hist{prefijo}");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, $"padre.{prefijo}@juntaly.test", hijo);
        var tag = $"E200{prefijo.ToUpperInvariant()}";
        await DatosDePrueba.CrearVehiculoAsync(_factory, padre, $"HIS{prefijo.Length:000}", tag);
        await DatosDePrueba.CrearFranjaAsync(_factory, colegio.Id, DayOfWeek.Monday, "12:00", "13:00");
        return (await DatosDePrueba.EstablecerClaveAntenaAsync(_factory, colegio.Id), tag);
    }

    private async Task LeerAsync(string clave, string tag)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/antena/detecciones") { Content = JsonContent.Create(new { tag }) };
        peticion.Headers.Add("X-Antena-Clave", clave);
        (await _factory.CreateClient().SendAsync(peticion)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Padre_VeElHistorialDeRecogidasDeSuHijo()
    {
        var (clave, tag) = await ColegioConFamiliaAsync("histpadre");
        await LeerAsync(clave, tag);
        var padre = await _factory.IniciarSesionAsync("padre.histpadre@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);

        var html = await padre.GetStringAsync("/Padres");

        Assert.Contains("Últimas recogidas", html);
        Assert.Contains("12:05", html);
        Assert.Contains("Pendiente de entregar", html);
    }

    [Fact]
    public async Task Supervisor_VeElRegistroDeDeteccionesConLasDeFueraDeFranja_SoloDeSuColegio()
    {
        var (clave, tag) = await ColegioConFamiliaAsync("regsup");
        var (claveOtro, tagOtro) = await ColegioConFamiliaAsync("regotro");
        await LeerAsync(clave, tag);
        _factory.Reloj.SetUtcNow(EnFranja.AddHours(8));
        await LeerAsync(clave, tag);
        await LeerAsync(claveOtro, tagOtro);
        var supervisor = await _factory.IniciarSesionAsync("sup.regsup@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);

        var html = await supervisor.GetStringAsync("/Colegio/Detecciones");

        Assert.Contains("Alumnos llamados", html);
        Assert.Contains("Fuera de franja", html);
        Assert.Contains(tag, html);
        Assert.DoesNotContain(tagOtro, html);
    }
}
