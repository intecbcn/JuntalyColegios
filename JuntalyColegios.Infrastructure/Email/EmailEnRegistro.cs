using JuntalyColegios.Application.Ports.Out;
using Microsoft.Extensions.Logging;

namespace JuntalyColegios.Infrastructure.Email;

/// <summary>
/// Emisor para desarrollo local sin servidor de correo: escribe el email en el log (consola), con su
/// contenido completo, para poder seguir los enlaces de invitación o de recuperación de contraseña.
/// </summary>
public sealed class EmailEnRegistro(ILogger<EmailEnRegistro> logger) : IEmailSender
{
    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Email NO enviado (sin SMTP en desarrollo). Para: {Destinatario} · Asunto: {Asunto}\n{Cuerpo}",
            destinatario, asunto, cuerpoHtml);
        return Task.CompletedTask;
    }
}
