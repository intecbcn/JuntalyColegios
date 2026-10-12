using JuntalyColegios.Application.Ports.Out;

namespace JuntalyColegios.Configuracion;

/// <summary>
/// Adaptador: las páginas de Identity UI (recuperar contraseña, reenviar confirmación, cambio de email)
/// envían correo con su propia interfaz. Sin este registro Identity usa un emisor que no hace nada y los
/// emails nunca salen; aquí se delegan en el puerto <see cref="IEmailSender"/> de la aplicación (SMTP).
/// </summary>
public sealed class EmailSenderIdentityUi(IEmailSender emailSender) : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        emailSender.EnviarAsync(email, subject, htmlMessage, CancellationToken.None);
}
