using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: persistencia de colegios.</summary>
public interface IRepositorioColegios
{
    Task AgregarAsync(Colegio colegio, CancellationToken cancellationToken);

    Task<Colegio?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Colegio>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>Persiste los cambios hechos sobre un colegio obtenido con <see cref="ObtenerAsync"/>.</summary>
    Task ActualizarAsync(Colegio colegio, CancellationToken cancellationToken);

    /// <summary>Colegio cuya clave de antena tiene ese hash (la antena se identifica con la clave).</summary>
    Task<Colegio?> ObtenerPorHashClaveAntenaAsync(string hash, CancellationToken cancellationToken);
}
