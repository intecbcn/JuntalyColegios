using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: lecturas de la antena (todas, generen o no llamadas).</summary>
public interface IRepositorioDetecciones
{
    /// <summary>Guarda la detección y las recogidas que generó, todo o nada.</summary>
    Task GuardarAsync(Deteccion deteccion, IReadOnlyCollection<Recogida> recogidas, CancellationToken cancellationToken);

    Task<IReadOnlyList<Deteccion>> ListarDeVehiculoDesdeAsync(Guid vehiculoId, DateTimeOffset desde, CancellationToken cancellationToken);

    /// <summary>Las <paramref name="maximo"/> lecturas más recientes del colegio, de la más nueva a la más antigua.</summary>
    Task<IReadOnlyList<Deteccion>> ListarRecientesAsync(Guid colegioId, int maximo, CancellationToken cancellationToken);
}

/// <summary>Puerto de salida: recogidas (alumnos llamados a la salida).</summary>
public interface IRepositorioRecogidas
{
    Task<Recogida?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Recogida>> ListarPendientesAsync(Guid colegioId, CancellationToken cancellationToken);

    Task ActualizarAsync(Recogida recogida, CancellationToken cancellationToken);

    /// <summary>Las <paramref name="maximo"/> recogidas más recientes de esos alumnos, de la más nueva a la más antigua.</summary>
    Task<IReadOnlyList<Recogida>> ListarRecientesDeAlumnosAsync(IReadOnlyCollection<Guid> alumnoIds, int maximo, CancellationToken cancellationToken);
}
