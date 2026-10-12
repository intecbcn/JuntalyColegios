using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: personas autorizadas por un representante a recoger a sus hijos.</summary>
public interface IRepositorioAutorizados
{
    Task AgregarAsync(PersonaAutorizada autorizada, CancellationToken cancellationToken);

    Task<PersonaAutorizada?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PersonaAutorizada>> ListarPorRepresentanteAsync(Guid representanteId, CancellationToken cancellationToken);

    Task EliminarAsync(PersonaAutorizada autorizada, CancellationToken cancellationToken);
}
