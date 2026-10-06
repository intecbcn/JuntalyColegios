using System.Collections.Concurrent;
using JuntalyColegios.Application.Ports.Out;

namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>
/// Doble de prueba del puerto <see cref="IEmailSender"/>: registra los correos en memoria en vez de
/// enviarlos por SMTP. Permite simular un fallo de envío con <see cref="FallarEnvios"/>.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<CorreoEnviado> _enviados = new();

    public IReadOnlyCollection<CorreoEnviado> Enviados => _enviados.ToArray();

    public bool FallarEnvios { get; set; }

    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken)
    {
        if (FallarEnvios)
            throw new InvalidOperationException("Fallo simulado de SMTP.");

        _enviados.Enqueue(new CorreoEnviado(destinatario, asunto, cuerpoHtml));
        return Task.CompletedTask;
    }

    public void Reiniciar()
    {
        _enviados.Clear();
        FallarEnvios = false;
    }
}

public sealed record CorreoEnviado(string Destinatario, string Asunto, string CuerpoHtml);
