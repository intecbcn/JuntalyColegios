using System.Net;
using System.Text.RegularExpressions;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Integration;

/// <summary>Alta de padres, vínculo con hijos y tags RFID desde el área del supervisor.</summary>
public partial class SupervisorPadresIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public SupervisorPadresIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EmailSender.Reiniciar();
    }

    [GeneratedRegex("/Colegio/Representantes/Detalle/([0-9a-fA-F-]{36})")]
    private static partial Regex RutaDetalle();

    private Task<HttpClient> SupervisorAsync(string email) =>
        _factory.IniciarSesionAsync(email, AutenticacionTests.ContrasenaPorDefecto);

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient cliente, string urlFormulario, string urlPost, IEnumerable<KeyValuePair<string, string>> campos)
    {
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync(urlFormulario));
        return await cliente.PostAsync(urlPost, new FormUrlEncodedContent(
            campos.Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));
    }

    [Fact]
    public async Task AreaPadres_UsuarioPadre_AccesoDenegado()
    {
        var padre = await _factory.CrearClienteAutenticadoAsync("padre.sin.acceso@juntaly.test", Roles.Padre);

        var respuesta = await padre.GetAsync("/Colegio/Representantes");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AltaPadre_ConHijo_CreaCuentaPadreEnviaInvitacionYMuestraAlHijo()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.padres@juntaly.test");
        var clase = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "4.º A");
        var hijo = await DatosDePrueba.CrearAlumnoAsync(_factory, clase, "Daniela", "Rojas");
        var supervisor = await SupervisorAsync("sup.padres@juntaly.test");

        var respuesta = await EnviarFormularioAsync(supervisor, "/Colegio/Representantes/Crear", "/Colegio/Representantes/Crear",
        [
            new("Nombre", "Andrés Rojas"),
            new("Email", "andres.rojas@juntaly.test"),
            new("Telefono", "+58 414 0000000"),
            new("AlumnoIds", hijo.Id.ToString())
        ]);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var id = RutaDetalle().Match(respuesta.Headers.Location!.OriginalString).Groups[1].Value;
        var detalle = await supervisor.GetStringAsync($"/Colegio/Representantes/Detalle/{id}");
        Assert.Contains("Daniela Rojas", detalle);
        Assert.Contains("4.º A", detalle);

        using var scope = _factory.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var cuenta = await usuarios.FindByEmailAsync("andres.rojas@juntaly.test");
        Assert.NotNull(cuenta);
        Assert.True(await usuarios.IsInRoleAsync(cuenta, Roles.Padre));
        Assert.Contains(_factory.EmailSender.Enviados, c => c.Destinatario == "andres.rojas@juntaly.test" && c.CuerpoHtml.Contains("ResetPassword"));
    }

    [Fact]
    public async Task AgregarVehiculoYAsignarTag_SeGuardaYSeMuestra()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.tags@juntaly.test");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.tags@juntaly.test");
        var supervisor = await SupervisorAsync("sup.tags@juntaly.test");
        var urlDetalle = $"/Colegio/Representantes/Detalle/{padre.Id}";

        await EnviarFormularioAsync(supervisor, urlDetalle, $"/Colegio/Representantes/AgregarVehiculo/{padre.Id}",
            [new("NuevoVehiculo.Placa", "ab 123 cd"), new("NuevoVehiculo.Descripcion", "Corolla gris")]);
        Guid vehiculoId = default;
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            vehiculoId = (await db.Vehiculos.SingleAsync(v => v.RepresentanteId == padre.Id)).Id);

        await EnviarFormularioAsync(supervisor, urlDetalle, $"/Colegio/Representantes/AsignarTag/{padre.Id}",
            [new("vehiculoId", vehiculoId.ToString()), new("tag", "e2004700a1b2")]);

        var detalle = await supervisor.GetStringAsync(urlDetalle);
        Assert.Contains("AB123CD", detalle);
        Assert.Contains("E2004700A1B2", detalle);
    }

    [Fact]
    public async Task AsignarTag_YaUsadoEnElColegio_MuestraElErrorYNoLoAsigna()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.duplicado@juntaly.test");
        var padre1 = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.dup1@juntaly.test");
        var padre2 = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.dup2@juntaly.test");
        await DatosDePrueba.CrearVehiculoAsync(_factory, padre1, "AAA111", "E200DUPLICADO1");
        var vehiculo2 = await DatosDePrueba.CrearVehiculoAsync(_factory, padre2, "BBB222");
        var supervisor = await SupervisorAsync("sup.duplicado@juntaly.test");
        var urlDetalle = $"/Colegio/Representantes/Detalle/{padre2.Id}";

        await EnviarFormularioAsync(supervisor, urlDetalle, $"/Colegio/Representantes/AsignarTag/{padre2.Id}",
            [new("vehiculoId", vehiculo2.Id.ToString()), new("tag", "E200DUPLICADO1")]);

        Assert.Contains("AAA111", await supervisor.GetStringAsync(urlDetalle));
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.Null((await db.Vehiculos.AsNoTracking().SingleAsync(v => v.Id == vehiculo2.Id)).Tag));
    }

    [Fact]
    public async Task Supervisor_PadreYVehiculoDeOtroColegio_NoLosVeNiLosModifica()
    {
        var colegioA = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.padres.a@juntaly.test", "Colegio A");
        var colegioB = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.padres.b@juntaly.test", "Colegio B");
        await DatosDePrueba.CrearRepresentanteAsync(_factory, colegioA.Id, "padre.de.a@juntaly.test");
        var padreB = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegioB.Id, "padre.de.b@juntaly.test");
        var vehiculoB = await DatosDePrueba.CrearVehiculoAsync(_factory, padreB, "ZZZ999");
        var supervisorA = await SupervisorAsync("sup.padres.a@juntaly.test");

        var ver = await supervisorA.GetAsync($"/Colegio/Representantes/Detalle/{padreB.Id}");
        var listado = await supervisorA.GetStringAsync("/Colegio/Representantes");
        var tag = await EnviarFormularioAsync(supervisorA, "/Colegio/Representantes", $"/Colegio/Representantes/AsignarTag/{padreB.Id}",
            [new("vehiculoId", vehiculoB.Id.ToString()), new("tag", "E200INTRUSO")]);

        Assert.Equal(HttpStatusCode.NotFound, ver.StatusCode);
        Assert.DoesNotContain("padre.de.b@juntaly.test", listado);
        Assert.Contains("padre.de.a@juntaly.test", listado);
        Assert.Equal(HttpStatusCode.NotFound, tag.StatusCode);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.Null((await db.Vehiculos.AsNoTracking().SingleAsync(v => v.Id == vehiculoB.Id)).Tag));
    }
}
