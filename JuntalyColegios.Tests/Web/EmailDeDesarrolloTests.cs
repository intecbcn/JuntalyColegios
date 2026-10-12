using JuntalyColegios.Configuracion;
using JuntalyColegios.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JuntalyColegios.Tests.Web;

/// <summary>
/// Para probar la app en local sin servidor de correo: en Development y sin SMTP configurado, los
/// emails (con el enlace de invitación) se escriben en el log en vez de fallar. En cualquier otro
/// entorno se usa SMTP y su ausencia es un error visible.
/// </summary>
public class EmailDeDesarrolloTests
{
    private static IConfiguration Config(string? hostSmtp) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Smtp:Host"] = hostSmtp }).Build();

    private static IHostEnvironment Entorno(string nombre)
    {
        var entorno = Substitute.For<IHostEnvironment>();
        entorno.EnvironmentName.Returns(nombre);
        return entorno;
    }

    [Theory]
    [InlineData("Development", null, typeof(EmailEnRegistro))]
    [InlineData("Development", "", typeof(EmailEnRegistro))]
    [InlineData("Development", "smtp.ejemplo.com", typeof(SmtpEmailSender))]
    [InlineData("Production", null, typeof(SmtpEmailSender))]
    public void ElegirEmisor_SoloEnDesarrolloSinSmtp_EscribeEnElLog(string entorno, string? host, Type esperado)
    {
        Assert.Equal(esperado, EmisorDeEmail.Elegir(Config(host), Entorno(entorno)));
    }

    [Fact]
    public async Task EmailEnRegistro_EscribeDestinatarioAsuntoYCuerpoConElEnlace()
    {
        var logger = new LoggerDePrueba();
        var sut = new EmailEnRegistro(logger);

        await sut.EnviarAsync("luis@colegio.com", "Bienvenido", "<a href=\"https://localhost/Identity/Account/ResetPassword?code=X\">Establecer</a>", CancellationToken.None);

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.Contains("luis@colegio.com", mensaje);
        Assert.Contains("Bienvenido", mensaje);
        Assert.Contains("ResetPassword?code=X", mensaje);
    }

    private sealed class LoggerDePrueba : ILogger<EmailEnRegistro>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Mensajes.Add(formatter(state, exception));
    }
}
