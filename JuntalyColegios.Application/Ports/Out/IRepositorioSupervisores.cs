using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: persistencia de supervisores de colegio.</summary>
public interface IRepositorioSupervisores
{
    Task AgregarAsync(Supervisor supervisor, CancellationToken cancellationToken);

    Task<IReadOnlyList<Supervisor>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task<Supervisor?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken);

    Task<Supervisor?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task ActualizarAsync(Supervisor supervisor, CancellationToken cancellationToken);

    Task EliminarAsync(Supervisor supervisor, CancellationToken cancellationToken);
}
