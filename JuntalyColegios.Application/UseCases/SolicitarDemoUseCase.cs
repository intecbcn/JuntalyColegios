using System.Text.Encodings.Web;
using System.Text.Unicode;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;

namespace JuntalyColegios.Application.UseCases;

/// <summary>Caso de uso único: recibe la solicitud de demo y la envía a ventas por correo.</summary>
public sealed class SolicitarDemoUseCase : ISolicitarDemoUseCase
{
    private const string EmailVentas = "info@serviciosintec.com";

    // Escapa solo lo peligroso en HTML (<, >, &, comillas) y deja legibles tildes y eñes.
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    private readonly IEmailSender _emailSender;

    public SolicitarDemoUseCase(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    /// <summary>
    /// Envía la solicitud. Deja que una falla de envío se propague: quien orquesta la petición
    /// HTTP decide qué mostrarle al visitante y cómo registrar el error (no es responsabilidad
    /// del caso de uso ocultarlo ni registrarlo).
    /// </summary>
    public async Task<bool> EjecutarAsync(SolicitudDemoDto solicitud, CancellationToken cancellationToken)
    {
        // Los datos los escribe el visitante y el cuerpo es HTML: se escapan para que no puedan
        // inyectar marcado (enlaces, scripts) en el correo que recibe ventas.
        var cuerpo = $"""
            Nueva solicitud de demo — Juntaly Colegios<br><br>
            Nombre: {Html.Encode(solicitud.Nombre)}<br>
            Colegio: {Html.Encode(solicitud.Colegio)}<br>
            Teléfono o email: {Html.Encode(solicitud.Contacto)}<br>
            N.º de alumnos: {(solicitud.NumeroAlumnos.HasValue ? solicitud.NumeroAlumnos.Value.ToString() : "No indicado")}
            """;

        await _emailSender.EnviarAsync(EmailVentas, "Nueva solicitud de demo — Juntaly Colegios", cuerpo, cancellationToken);
        return true;
    }
}
