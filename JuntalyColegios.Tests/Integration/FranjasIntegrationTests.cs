using System.Net;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Tests.Integration;

/// <summary>Franjas de retiro configuradas por el supervisor sobre el pipeline real.</summary>
public class FranjasIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public FranjasIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpClient> SupervisorAsync(string email) =>
        _factory.IniciarSesionAsync(email, AutenticacionTests.ContrasenaPorDefecto);

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string url, IEnumerable<KeyValuePair<string, string>> campos)
    {
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Colegio/Franjas"));
        return await cliente.PostAsync(url, new FormUrlEncodedContent(campos.Append(new("__RequestVerificationToken", token))));
    }

    [Fact]
    public async Task SinFranjas_AvisaDeQueNoSeLlamaraANadie()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.sinfranjas@juntaly.test");
        var supervisor = await SupervisorAsync("sup.sinfranjas@juntaly.test");

        var html = await supervisor.GetStringAsync("/Colegio/Franjas");

        Assert.Contains("no se llamará a ningún alumno", html);
    }

    [Fact]
    public async Task CrearFranjas_LunesYViernes_SeGuardanYSeMuestraQueAhoraEstaEnFranja()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.franjas@juntaly.test");
        var supervisor = await SupervisorAsync("sup.franjas@juntaly.test");

        var respuesta = await EnviarAsync(supervisor, "/Colegio/Franjas/Crear",
        [
            new("ClaseId", ""),
            new("Dias", "Monday"),
            new("Dias", "Friday"),
            new("HoraInicio", "12:00"),
            new("HoraFin", "13:00")
        ]);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var html = await supervisor.GetStringAsync("/Colegio/Franjas");
        Assert.Contains("Lunes", html);
        Assert.Contains("Viernes", html);
        Assert.Contains("12:00", html);
        // El reloj de los tests marca lunes 12:05 en Caracas.
        Assert.Contains("Ahora se llama a los alumnos", html);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.Equal(2, await db.FranjasRetiro.CountAsync(f => f.ColegioId == colegio.Id)));
    }

    [Fact]
    public async Task EliminarFranja_DeOtroColegio_404YNoSeElimina()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.franjas.a@juntaly.test", "Colegio A");
        var colegioB = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.franjas.b@juntaly.test", "Colegio B");
        var ajena = FranjaRetiro.Crear(colegioB.Id, null, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(13, 0));
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, db =>
        {
            db.FranjasRetiro.Add(ajena);
            return db.SaveChangesAsync();
        });
        var supervisorA = await SupervisorAsync("sup.franjas.a@juntaly.test");

        var respuesta = await EnviarAsync(supervisorA, $"/Colegio/Franjas/Eliminar/{ajena.Id}", []);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db => Assert.True(await db.FranjasRetiro.AnyAsync(f => f.Id == ajena.Id)));
    }
}
