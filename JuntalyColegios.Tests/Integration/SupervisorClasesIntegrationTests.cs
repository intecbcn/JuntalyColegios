using System.Net;
using System.Text.RegularExpressions;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Clases y alumnos del supervisor sobre el pipeline real. Lo esencial: un supervisor solo ve y toca los
/// datos de su colegio (lo ajeno devuelve 404, sin revelar que existe).
/// </summary>
public partial class SupervisorClasesIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public SupervisorClasesIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [GeneratedRegex("/Colegio/Clases/Detalle/([0-9a-fA-F-]{36})")]
    private static partial Regex RutaDetalleClase();

    private async Task<HttpClient> SupervisorAsync(string email) =>
        await _factory.IniciarSesionAsync(email, AutenticacionTests.ContrasenaPorDefecto);

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient cliente, string urlFormulario, string urlPost, Dictionary<string, string> campos)
    {
        campos["__RequestVerificationToken"] = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync(urlFormulario));
        return await cliente.PostAsync(urlPost, new FormUrlEncodedContent(campos));
    }

    [Fact]
    public async Task AreaColegio_Padre_AccesoDenegado()
    {
        var padre = await _factory.CrearClienteAutenticadoAsync("padre.area@juntaly.test", Roles.Padre);

        var respuesta = await padre.GetAsync("/Colegio");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AreaColegio_RolSupervisorSinColegioAsignado_AccesoDenegado()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("supervisor.sin.colegio@juntaly.test", Roles.SupervisorColegio);

        var respuesta = await cliente.GetAsync("/Colegio");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AreaColegio_ColegioDesactivado_AccesoDenegado()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.desactivado@juntaly.test", activo: false);
        var cliente = await SupervisorAsync("sup.desactivado@juntaly.test");

        var respuesta = await cliente.GetAsync("/Colegio");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Supervisor_CreaClaseYAlumno_SeGuardanEnSuColegioYSeVen()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.crea@juntaly.test", "Colegio Bolívar");
        var cliente = await SupervisorAsync("sup.crea@juntaly.test");

        Assert.Contains("Colegio Bolívar", await cliente.GetStringAsync("/Colegio"));

        var respuesta = await EnviarFormularioAsync(cliente, "/Colegio/Clases/Crear", "/Colegio/Clases/Crear", new()
        {
            ["Nombre"] = "6.º A",
            ["Nivel"] = "Primaria"
        });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var claseId = Guid.Parse(RutaDetalleClase().Match(respuesta.Headers.Location!.OriginalString).Groups[1].Value);

        respuesta = await EnviarFormularioAsync(cliente, $"/Colegio/Clases/Detalle/{claseId}", "/Colegio/Alumnos/Crear", new()
        {
            ["NuevoAlumno.ClaseId"] = claseId.ToString(),
            ["NuevoAlumno.Nombres"] = "Valentina",
            ["NuevoAlumno.Apellidos"] = "Núñez"
        });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Assert.Contains("Valentina", await cliente.GetStringAsync($"/Colegio/Clases/Detalle/{claseId}"));
        Assert.Contains("6.º A", await cliente.GetStringAsync("/Colegio"));
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
        {
            var alumno = await db.Alumnos.SingleAsync(a => a.Nombres == "Valentina");
            Assert.Equal(colegio.Id, alumno.ColegioId);
            Assert.Equal(claseId, alumno.ClaseId);
        });
    }

    [Fact]
    public async Task Supervisor_ClaseDeOtroColegio_NoLaVeNiLaModifica()
    {
        var colegioA = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.a@juntaly.test", "Colegio A");
        var colegioB = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.b@juntaly.test", "Colegio B");
        var claseB = await DatosDePrueba.CrearClaseAsync(_factory, colegioB.Id, "Clase secreta de B");
        await DatosDePrueba.CrearClaseAsync(_factory, colegioA.Id, "Clase de A");
        var supervisorA = await SupervisorAsync("sup.a@juntaly.test");

        var ver = await supervisorA.GetAsync($"/Colegio/Clases/Detalle/{claseB.Id}");
        var listado = await supervisorA.GetStringAsync("/Colegio");
        var editar = await EnviarFormularioAsync(supervisorA, "/Colegio/Clases/Crear", $"/Colegio/Clases/Editar/{claseB.Id}", new()
        {
            ["Nombre"] = "Hackeada"
        });
        var alumnoEnB = await EnviarFormularioAsync(supervisorA, "/Colegio/Clases/Crear", "/Colegio/Alumnos/Crear", new()
        {
            ["NuevoAlumno.ClaseId"] = claseB.Id.ToString(),
            ["NuevoAlumno.Nombres"] = "Intruso",
            ["NuevoAlumno.Apellidos"] = "Ajeno"
        });
        var eliminar = await EnviarFormularioAsync(supervisorA, "/Colegio/Clases/Crear", $"/Colegio/Clases/Eliminar/{claseB.Id}", new());

        Assert.Equal(HttpStatusCode.NotFound, ver.StatusCode);
        Assert.DoesNotContain("Clase secreta de B", listado);
        Assert.Equal(HttpStatusCode.NotFound, editar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, alumnoEnB.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, eliminar.StatusCode);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
        {
            var clase = await db.Clases.AsNoTracking().SingleAsync(c => c.Id == claseB.Id);
            Assert.Equal("Clase secreta de B", clase.Nombre);
            Assert.False(await db.Alumnos.AnyAsync(a => a.Nombres == "Intruso"));
        });
    }

    [Fact]
    public async Task CrearClase_SinTokenAntiforgery_Rechazado()
    {
        await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.token@juntaly.test");
        var cliente = await SupervisorAsync("sup.token@juntaly.test");

        var respuesta = await cliente.PostAsync("/Colegio/Clases/Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombre"] = "Sin token"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
