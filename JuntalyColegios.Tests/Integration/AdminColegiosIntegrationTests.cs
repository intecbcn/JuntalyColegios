using System.Net;
using System.Text.RegularExpressions;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Infrastructure.Persistence;
using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Gestión de colegios y alta de supervisores por el administrador, sobre el pipeline real
/// (rutas del área Admin, antiforgery, autorización por rol, EF Core con SQLite e Identity).
/// </summary>
public partial class AdminColegiosIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public AdminColegiosIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EmailSender.Reiniciar();
    }

    [GeneratedRegex("/Admin/Colegios/Detalle/([0-9a-fA-F-]{36})")]
    private static partial Regex RutaDetalle();

    [GeneratedRegex("https://colegios\\.test/Identity/Account/ResetPassword\\?code=([A-Za-z0-9_-]+)")]
    private static partial Regex EnlaceInvitacion();

    [GeneratedRegex("name=\"Input.Code\"[^>]*value=\"([^\"]+)\"|value=\"([^\"]+)\"[^>]*name=\"Input.Code\"")]
    private static partial Regex CampoCodigo();

    private Task<HttpClient> ClienteAdminAsync() =>
        _factory.CrearClienteAutenticadoAsync("admin.colegios@juntaly.test", Roles.AdminApp);

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient cliente, string urlFormulario, string urlPost, Dictionary<string, string> campos)
    {
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync(urlFormulario));
        campos["__RequestVerificationToken"] = token;
        return await cliente.PostAsync(urlPost, new FormUrlEncodedContent(campos));
    }

    private static async Task<Guid> CrearColegioAsync(HttpClient cliente, string nombre)
    {
        var respuesta = await EnviarFormularioAsync(cliente, "/Admin/Colegios/Crear", "/Admin/Colegios/Crear", new()
        {
            ["Nombre"] = nombre,
            ["Direccion"] = "Av. Principal 1",
            ["ZonaHoraria"] = "America/Caracas"
        });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        return Guid.Parse(RutaDetalle().Match(respuesta.Headers.Location!.OriginalString).Groups[1].Value);
    }

    [Theory]
    [InlineData("/Admin/Colegios")]
    [InlineData("/Admin/Colegios/Crear")]
    public async Task PaginasDeColegios_SupervisorOPadre_AccesoDenegado(string url)
    {
        var supervisor = await _factory.CrearClienteAutenticadoAsync("supervisor.colegios@juntaly.test", Roles.SupervisorColegio);

        var respuesta = await supervisor.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task CrearColegio_Admin_ApareceEnElListadoYEnLaBaseDeDatos()
    {
        var admin = await ClienteAdminAsync();

        var id = await CrearColegioAsync(admin, "Colegio Integración");

        var listado = await admin.GetStringAsync("/Admin/Colegios");
        Assert.Contains("Colegio Integración", listado);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Colegios.AnyAsync(c => c.Id == id && c.Nombre == "Colegio Integración" && c.Activo));
    }

    [Fact]
    public async Task CrearColegio_SinTokenAntiforgery_Rechazado()
    {
        var admin = await ClienteAdminAsync();

        var respuesta = await admin.PostAsync("/Admin/Colegios/Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombre"] = "Colegio sin token"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearColegio_NombreVacio_MuestraErrorDeValidacion()
    {
        var admin = await ClienteAdminAsync();

        var respuesta = await EnviarFormularioAsync(admin, "/Admin/Colegios/Crear", "/Admin/Colegios/Crear", new()
        {
            ["Nombre"] = "",
            ["ZonaHoraria"] = "America/Caracas"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("field-validation-error", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EditarYDesactivarColegio_Admin_PersisteLosCambios()
    {
        var admin = await ClienteAdminAsync();
        var id = await CrearColegioAsync(admin, "Colegio a editar");

        await EnviarFormularioAsync(admin, $"/Admin/Colegios/Editar/{id}", $"/Admin/Colegios/Editar/{id}", new()
        {
            ["Nombre"] = "Colegio editado",
            ["Direccion"] = "Calle 2",
            ["ZonaHoraria"] = "America/Bogota"
        });
        await EnviarFormularioAsync(admin, $"/Admin/Colegios/Detalle/{id}", $"/Admin/Colegios/CambiarEstado/{id}", new()
        {
            ["activo"] = "false"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var colegio = await db.Colegios.AsNoTracking().SingleAsync(c => c.Id == id);
        Assert.Equal("Colegio editado", colegio.Nombre);
        Assert.Equal("America/Bogota", colegio.ZonaHoraria);
        Assert.False(colegio.Activo);
    }

    [Fact]
    public async Task Detalle_ColegioInexistente_Devuelve404()
    {
        var admin = await ClienteAdminAsync();

        var respuesta = await admin.GetAsync($"/Admin/Colegios/Detalle/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task AltaSupervisor_CreaCuentaConRolEnviaInvitacion_YElSupervisorPuedeEstablecerSuContrasenaEIniciarSesion()
    {
        var admin = await ClienteAdminAsync();
        var id = await CrearColegioAsync(admin, "Colegio con supervisor");

        var respuesta = await EnviarFormularioAsync(admin, $"/Admin/Colegios/Detalle/{id}", $"/Admin/Colegios/AltaSupervisor/{id}", new()
        {
            ["NuevoSupervisor.Nombre"] = "Luis Gómez",
            ["NuevoSupervisor.Email"] = "luis.supervisor@juntaly.test"
        });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        // La cuenta existe con el rol de supervisor y aparece en el detalle del colegio.
        using (var scope = _factory.Services.CreateScope())
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var usuario = await usuarios.FindByEmailAsync("luis.supervisor@juntaly.test");
            Assert.NotNull(usuario);
            Assert.True(await usuarios.IsInRoleAsync(usuario, Roles.SupervisorColegio));
        }
        Assert.Contains("luis.supervisor@juntaly.test", await admin.GetStringAsync($"/Admin/Colegios/Detalle/{id}"));

        // Recibe una invitación con un enlace para establecer su contraseña…
        var correo = Assert.Single(_factory.EmailSender.Enviados, c => c.Destinatario == "luis.supervisor@juntaly.test");
        var codigoUrl = EnlaceInvitacion().Match(correo.CuerpoHtml).Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(codigoUrl), "La invitación debe incluir el enlace para establecer la contraseña.");

        // …que funciona: establece la contraseña e inicia sesión.
        var anonimo = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var formulario = await anonimo.GetStringAsync($"/Identity/Account/ResetPassword?code={codigoUrl}");
        var codigo = WebUtility.HtmlDecode(CampoCodigo().Match(formulario) is { Success: true } m
            ? (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            : string.Empty);
        var restablecer = await anonimo.PostAsync("/Identity/Account/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "luis.supervisor@juntaly.test",
            ["Input.Password"] = "Supervisor123!",
            ["Input.ConfirmPassword"] = "Supervisor123!",
            ["Input.Code"] = codigo,
            ["__RequestVerificationToken"] = AutenticacionTests.ExtraerTokenAntiforgery(formulario)
        }));
        Assert.Equal(HttpStatusCode.Redirect, restablecer.StatusCode);

        await _factory.IniciarSesionAsync("luis.supervisor@juntaly.test", "Supervisor123!");
    }

    [Fact]
    public async Task AltaSupervisor_EmailYaRegistrado_NoCreaOtraCuentaYMuestraElError()
    {
        var admin = await ClienteAdminAsync();
        var id = await CrearColegioAsync(admin, "Colegio duplicado");
        await AutenticacionTests.CrearUsuarioAsync(_factory, "ya.existe@juntaly.test");

        await EnviarFormularioAsync(admin, $"/Admin/Colegios/Detalle/{id}", $"/Admin/Colegios/AltaSupervisor/{id}", new()
        {
            ["NuevoSupervisor.Nombre"] = "Repetido",
            ["NuevoSupervisor.Email"] = "ya.existe@juntaly.test"
        });

        var detalle = await admin.GetStringAsync($"/Admin/Colegios/Detalle/{id}");
        Assert.Contains("Ya existe una cuenta con ese email", detalle);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Supervisores.AnyAsync(s => s.ColegioId == id));
        Assert.DoesNotContain(_factory.EmailSender.Enviados, c => c.Destinatario == "ya.existe@juntaly.test");
    }
}
