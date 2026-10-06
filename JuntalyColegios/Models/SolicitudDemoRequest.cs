namespace JuntalyColegios.Models;

/// <summary>Modelo de binding del formulario "Solicita una demo" (capa Web).</summary>
public class SolicitudDemoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Colegio { get; set; } = string.Empty;

    /// <summary>Teléfono o email de contacto.</summary>
    public string Contacto { get; set; } = string.Empty;

    public int? NumeroAlumnos { get; set; }

    /// <summary>
    /// Honeypot anti-bots: campo oculto por CSS que un visitante real nunca ve ni rellena. Si llega
    /// con contenido, la solicitud se descarta antes de tocar el caso de uso. No es un dato de
    /// negocio, por eso vive solo aquí y no en el DTO de Application.
    /// </summary>
    public string? Empresa2 { get; set; }
}
