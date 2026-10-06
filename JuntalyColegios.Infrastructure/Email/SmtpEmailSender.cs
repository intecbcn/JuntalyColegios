using System.Net;
using System.Net.Mail;
using JuntalyColegios.Application.Ports.Out;
using Microsoft.Extensions.Configuration;

namespace JuntalyColegios.Infrastructure.Email;

/// <summary>
/// Adaptador de salida: implementa <see cref="IEmailSender"/> con SMTP. La configuración se lee de
/// appsettings/User Secrets/variables de entorno bajo la clave "Smtp" — nunca hardcodeada (ver
/// AGENTS.md: no commitear secretos ni cadenas de conexión reales).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken)
    {
        var host = _configuration["Smtp:Host"];
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? user;
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Falta configurar Smtp:Host/User/Password (appsettings o User Secrets).");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password)
        };
        using var mensaje = new MailMessage(from!, destinatario, asunto, cuerpoHtml) { IsBodyHtml = true };

        await client.SendMailAsync(mensaje, cancellationToken);
    }
}
