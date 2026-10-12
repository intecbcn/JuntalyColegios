using System.Globalization;
using System.Threading.RateLimiting;
using JuntalyColegios.Application.UseCases.Salida;
using JuntalyColegios.Controllers;
using Microsoft.AspNetCore.RateLimiting;

namespace JuntalyColegios.Configuracion;

/// <summary>
/// Límite de lecturas por minuto de la API pública de la antena. Se cuenta por antena (clave del
/// colegio), no por IP: detrás de un proxy o CDN todas las peticiones llegan con sus IPs y un colegio
/// no debe consumir el cupo de otro. Superado el límite se responde 429 con Retry-After.
/// </summary>
public static class LimiteAntena
{
    public const string Politica = "antena";

    /// <summary>Holgado para una antena real: un carro se lee varias veces mientras avanza en la fila.</summary>
    public const int PeticionesPorMinutoPorDefecto = 120;

    public static IServiceCollection AddLimiteAntena(this IServiceCollection services, IConfiguration configuracion)
    {
        var porMinuto = configuracion.GetValue("Antena:PeticionesPorMinuto", PeticionesPorMinutoPorDefecto);

        return services.AddRateLimiter(opciones =>
        {
            opciones.AddPolicy(Politica, http =>
            {
                // Se particiona por el hash de la clave: el limitador no guarda la clave en claro.
                var clave = http.Request.Headers[AntenaController.CabeceraClave].ToString();
                var particion = string.IsNullOrEmpty(clave) ? "sin-clave" : ClaveDeAntena.Hash(clave);
                return RateLimitPartition.GetFixedWindowLimiter(particion, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = porMinuto,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = (contexto, _) =>
            {
                var segundos = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera)
                    ? (int)Math.Ceiling(espera.TotalSeconds)
                    : 60;
                contexto.HttpContext.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };
        });
    }
}
