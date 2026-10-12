using System.Net;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Tests.Integration;

/// <summary>Panel de padres sobre el pipeline real: cada familia ve y gestiona solo lo suyo.</summary>
public class PanelPadresIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public PanelPadresIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpClient> PadreAsync(string email) =>
        _factory.IniciarSesionAsync(email, AutenticacionTests.ContrasenaPorDefecto);

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string url, Dictionary<string, string> campos)
    {
        campos["__RequestVerificationToken"] = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Padres"));
        return await cliente.PostAsync(url, new FormUrlEncodedContent(campos));
    }

    [Fact]
    public async Task PanelPadres_Supervisor_AccesoDenegado()
    {
        var supervisor = await _factory.CrearClienteAutenticadoAsync("supervisor.panel@juntaly.test", Roles.SupervisorColegio);

        var respuesta = await supervisor.GetAsync("/Padres");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task PanelPadres_RolPadreSinFamiliaRegistrada_AccesoDenegado()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync("padre.sin.familia@juntaly.test", Roles.Padre);

        var respuesta = await cliente.GetAsync("/Padres");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task PanelPadres_MuestraSusHijosYVehiculos_NoLosDeOtraFamilia()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.panel@juntaly.test", "Colegio Miranda");
        var clase = await DatosDePrueba.CrearClaseAsync(_factory, colegio.Id, "5.º B");
        var hija = await DatosDePrueba.CrearAlumnoAsync(_factory, clase, "Lucía", "Méndez");
        var ajeno = await DatosDePrueba.CrearAlumnoAsync(_factory, clase, "Pedro", "Ajeno");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.panel@juntaly.test", hija);
        var otro = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "otro.panel@juntaly.test", ajeno);
        await DatosDePrueba.CrearVehiculoAsync(_factory, padre, "MIO111", "E200PROPIO");
        await DatosDePrueba.CrearVehiculoAsync(_factory, otro, "AJENO22");
        var cliente = await PadreAsync("padre.panel@juntaly.test");

        var html = await cliente.GetStringAsync("/Padres");

        Assert.Contains("Colegio Miranda", html);
        Assert.Contains("Lucía Méndez", html);
        Assert.Contains("5.º B", html);
        Assert.Contains("MIO111", html);
        Assert.Contains("E200PROPIO", html);
        Assert.DoesNotContain("Pedro", html);
        Assert.DoesNotContain("AJENO22", html);
    }

    [Fact]
    public async Task Padre_RegistraVehiculoYAutorizado_SeGuardanASuNombre()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.registra@juntaly.test");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.registra@juntaly.test");
        var cliente = await PadreAsync("padre.registra@juntaly.test");

        await EnviarAsync(cliente, "/Padres/Inicio/RegistrarVehiculo", new()
        {
            ["NuevoVehiculo.Placa"] = "xy 987 zz",
            ["NuevoVehiculo.Descripcion"] = "Spark rojo"
        });
        await EnviarAsync(cliente, "/Padres/Inicio/AgregarAutorizado", new()
        {
            ["NuevoAutorizado.Nombre"] = "Rosa Méndez",
            ["NuevoAutorizado.Documento"] = "V-9876543",
            ["NuevoAutorizado.Parentesco"] = "Abuela"
        });

        var html = await cliente.GetStringAsync("/Padres");
        Assert.Contains("XY987ZZ", html);
        Assert.Contains("Rosa Méndez", html);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
        {
            var vehiculo = await db.Vehiculos.SingleAsync(v => v.Placa == "XY987ZZ");
            Assert.Equal(padre.Id, vehiculo.RepresentanteId);
            Assert.Null(vehiculo.Tag);
            Assert.True(await db.PersonasAutorizadas.AnyAsync(a => a.RepresentanteId == padre.Id && a.Documento == "V-9876543"));
        });
    }

    [Fact]
    public async Task Padre_EliminarVehiculoDeOtraFamilia_404YNoSeElimina()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.eliminar@juntaly.test");
        await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.eliminar@juntaly.test");
        var otro = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "otro.eliminar@juntaly.test");
        var ajeno = await DatosDePrueba.CrearVehiculoAsync(_factory, otro, "NOTOCAR1");
        var cliente = await PadreAsync("padre.eliminar@juntaly.test");

        var respuesta = await EnviarAsync(cliente, "/Padres/Inicio/EliminarVehiculo", new() { ["vehiculoId"] = ajeno.Id.ToString() });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db => Assert.True(await db.Vehiculos.AnyAsync(v => v.Id == ajeno.Id)));
    }

    [Fact]
    public async Task Padre_NoPuedeAsignarseUnTag()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.notag@juntaly.test");
        var padre = await DatosDePrueba.CrearRepresentanteAsync(_factory, colegio.Id, "padre.notag@juntaly.test");
        var vehiculo = await DatosDePrueba.CrearVehiculoAsync(_factory, padre, "SINTAG1");
        var cliente = await PadreAsync("padre.notag@juntaly.test");

        var respuesta = await EnviarAsync(cliente, $"/Colegio/Representantes/AsignarTag/{padre.Id}", new()
        {
            ["vehiculoId"] = vehiculo.Id.ToString(),
            ["tag"] = "E200ROBADO"
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db =>
            Assert.Null((await db.Vehiculos.AsNoTracking().SingleAsync(v => v.Id == vehiculo.Id)).Tag));
    }
}
