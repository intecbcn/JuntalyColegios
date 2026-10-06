using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

/// <summary>Puerto de entrada: procesa una solicitud de demo recibida desde la landing.</summary>
public interface ISolicitarDemoUseCase
{
    /// <summary>
    /// Envía la solicitud a ventas. Devuelve false si el envío falló (el llamador decide qué
    /// mostrarle al visitante; el caso de uso no oculta el resultado real).
    /// </summary>
    Task<bool> EjecutarAsync(SolicitudDemoDto solicitud, CancellationToken cancellationToken);
}
