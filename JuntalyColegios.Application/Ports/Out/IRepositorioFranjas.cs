using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: franjas horarias de retiro de los colegios.</summary>
public interface IRepositorioFranjas
{
    /// <summary>Guarda todas las franjas o ninguna.</summary>
    Task AgregarVariasAsync(IReadOnlyCollection<FranjaRetiro> franjas, CancellationToken cancellationToken);

    Task<FranjaRetiro?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FranjaRetiro>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task EliminarAsync(FranjaRetiro franja, CancellationToken cancellationToken);
}
