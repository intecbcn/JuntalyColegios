using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Llamada de un alumno a la salida porque llegó un vehículo de su familia. Queda pendiente (aparece en
/// la pantalla de salida) hasta que el personal lo entrega.
/// </summary>
public sealed class Recogida
{
    private Recogida()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    public Guid DeteccionId { get; private set; }

    public Guid VehiculoId { get; private set; }

    public Guid AlumnoId { get; private set; }

    public DateTimeOffset DetectadoEn { get; private set; }

    public DateTimeOffset? EntregadoEn { get; private set; }

    public bool Pendiente => EntregadoEn is null;

    public static Recogida Crear(Deteccion deteccion, Guid alumnoId)
    {
        if (deteccion.VehiculoId is not { } vehiculoId)
            throw new DominioException("Solo un vehículo reconocido puede generar una recogida.");

        return new Recogida
        {
            Id = Guid.NewGuid(),
            ColegioId = deteccion.ColegioId,
            DeteccionId = deteccion.Id,
            VehiculoId = vehiculoId,
            AlumnoId = alumnoId,
            DetectadoEn = deteccion.DetectadoEn
        };
    }

    public void MarcarEntregada(DateTimeOffset instante)
    {
        if (!Pendiente)
            throw new DominioException("El alumno ya estaba entregado.");
        EntregadoEn = instante;
    }
}
