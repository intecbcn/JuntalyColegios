namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: envío de correo. Implementado en Infrastructure.</summary>
public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken);
}
