using JuntalyColegios.Tests.Infrastructure;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Regresión: las páginas de Identity (¿Olvidaste tu contraseña?) usaban el IEmailSender propio de
/// Identity UI, que no estaba registrado (no-op), y el email de recuperación nunca se enviaba.
/// </summary>
public class RecuperarContrasenaIntegrationTests : IClassFixture<JuntalyWebApplicationFactory>
{
    private readonly JuntalyWebApplicationFactory _factory;

    public RecuperarContrasenaIntegrationTests(JuntalyWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EmailSender.Reiniciar();
    }

    [Fact]
    public async Task OlvideMiContrasena_UsuarioExistente_EnviaEmailConEnlaceDeRestablecimiento()
    {
        await AutenticacionTests.CrearUsuarioAsync(_factory, "olvido@juntaly.test");
        var cliente = _factory.CreateClient();
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Identity/Account/ForgotPassword"));

        await cliente.PostAsync("/Identity/Account/ForgotPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "olvido@juntaly.test",
            ["__RequestVerificationToken"] = token
        }));

        var correo = Assert.Single(_factory.EmailSender.Enviados, c => c.Destinatario == "olvido@juntaly.test");
        Assert.Contains("/Identity/Account/ResetPassword?code=", correo.CuerpoHtml);
    }

    [Fact]
    public async Task OlvideMiContrasena_HostAjeno_RechazaLaPeticionSinEnviarEnlace()
    {
        // El enlace del email se construye con el Host de la petición: con un Host falso, la víctima
        // recibiría un enlace al dominio del atacante con su token (password reset poisoning).
        await AutenticacionTests.CrearUsuarioAsync(_factory, "victima@juntaly.test");
        var cliente = _factory.CreateClient();
        var token = AutenticacionTests.ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Identity/Account/ForgotPassword"));
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/Identity/Account/ForgotPassword")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = "victima@juntaly.test",
                ["__RequestVerificationToken"] = token
            })
        };
        peticion.Headers.Host = "atacante.example";

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.DoesNotContain(_factory.EmailSender.Enviados, c => c.Destinatario == "victima@juntaly.test");
    }
}
