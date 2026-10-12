using System.Net;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Integration;

/// <summary>El administrador edita y da de baja supervisores de un colegio (pipeline real con Identity).</summary>
public class AdminSupervisoresIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public AdminSupervisoresIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpClient> AdminAsync() => _factory.CrearClienteAutenticadoAsync("admin.supervisores@juntaly.test", Roles.AdminApp);

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string urlFormulario, string urlPost, Dictionary<string, string> campos)
    {
        campos["__RequestVerificationToken"] = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync(urlFormulario));
        return await cliente.PostAsync(urlPost, new FormUrlEncodedContent(campos));
    }

    private async Task<Guid> SupervisorIdAsync(string email)
    {
        Guid id = default;
        await DatosDePrueba.EnBaseDeDatosAsync(_factory, async db => id = (await db.Supervisores.SingleAsync(s => s.Email == email)).Id);
        return id;
    }

    [Fact]
    public async Task EditarSupervisor_CambiaNombreYEmail_YPuedeEntrarConElEmailNuevo()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.editar@juntaly.test");
        var supervisorId = await SupervisorIdAsync("sup.editar@juntaly.test");
        var admin = await AdminAsync();
        var urlEditar = $"/Admin/Colegios/EditarSupervisor/{colegio.Id}?supervisorId={supervisorId}";

        var respuesta = await EnviarAsync(admin, urlEditar, urlEditar, new()
        {
            ["Nombre"] = "Supervisora Renombrada",
            ["Email"] = "sup.editada@juntaly.test"
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var detalle = await admin.GetStringAsync($"/Admin/Colegios/Detalle/{colegio.Id}");
        Assert.Contains("Supervisora Renombrada", detalle);
        Assert.Contains("sup.editada@juntaly.test", detalle);
        await _factory.IniciarSesionAsync("sup.editada@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);
    }

    [Fact]
    public async Task EditarSupervisor_EmailDeOtraCuenta_MuestraElErrorYNoCambia()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.noeditar@juntaly.test");
        await AutenticacionTests.CrearUsuarioAsync(_factory, "cuenta.ocupada@juntaly.test");
        var supervisorId = await SupervisorIdAsync("sup.noeditar@juntaly.test");
        var admin = await AdminAsync();
        var urlEditar = $"/Admin/Colegios/EditarSupervisor/{colegio.Id}?supervisorId={supervisorId}";

        var respuesta = await EnviarAsync(admin, urlEditar, urlEditar, new()
        {
            ["Nombre"] = "Supervisor",
            ["Email"] = "cuenta.ocupada@juntaly.test"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Ya existe una cuenta con ese email", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(supervisorId, await SupervisorIdAsync("sup.noeditar@juntaly.test"));
    }

    [Fact]
    public async Task EliminarSupervisor_DesapareceYSuCuentaYaNoExiste()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.eliminar.admin@juntaly.test");
        var supervisorId = await SupervisorIdAsync("sup.eliminar.admin@juntaly.test");
        var admin = await AdminAsync();
        var urlDetalle = $"/Admin/Colegios/Detalle/{colegio.Id}";

        var respuesta = await EnviarAsync(admin, urlDetalle, $"/Admin/Colegios/EliminarSupervisor/{colegio.Id}", new()
        {
            ["supervisorId"] = supervisorId.ToString()
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.DoesNotContain("sup.eliminar.admin@juntaly.test", await admin.GetStringAsync(urlDetalle));
        using var scope = _factory.Services.CreateScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync("sup.eliminar.admin@juntaly.test"));
    }

    [Fact]
    public async Task EliminarSupervisor_ConElIdDeOtroColegio_404YNoSeElimina()
    {
        var colegioA = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.colegio.a@juntaly.test", "Colegio A");
        await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.colegio.b@juntaly.test", "Colegio B");
        var supervisorB = await SupervisorIdAsync("sup.colegio.b@juntaly.test");
        var admin = await AdminAsync();

        var respuesta = await EnviarAsync(admin, $"/Admin/Colegios/Detalle/{colegioA.Id}", $"/Admin/Colegios/EliminarSupervisor/{colegioA.Id}", new()
        {
            ["supervisorId"] = supervisorB.ToString()
        });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(supervisorB, await SupervisorIdAsync("sup.colegio.b@juntaly.test"));
    }

    [Fact]
    public async Task EliminarSupervisor_SupervisorNoPuedeHacerlo()
    {
        var colegio = await DatosDePrueba.CrearColegioConSupervisorAsync(_factory, "sup.intruso@juntaly.test");
        var supervisor = await _factory.IniciarSesionAsync("sup.intruso@juntaly.test", AutenticacionTests.ContrasenaPorDefecto);

        var respuesta = await supervisor.GetAsync($"/Admin/Colegios/EditarSupervisor/{colegio.Id}?supervisorId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }
}
