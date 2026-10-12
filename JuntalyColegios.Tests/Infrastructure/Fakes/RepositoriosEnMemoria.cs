using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Tests.Infrastructure.Fakes;

/// <summary>Repositorio en memoria intercambiable por el de EF Core (Liskov) para tests de casos de uso.</summary>
public sealed class RepositorioColegiosEnMemoria : IRepositorioColegios
{
    public List<Colegio> Colegios { get; } = [];

    public int Actualizaciones { get; private set; }

    public Task AgregarAsync(Colegio colegio, CancellationToken cancellationToken)
    {
        Colegios.Add(colegio);
        return Task.CompletedTask;
    }

    public Task<Colegio?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Colegios.SingleOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Colegio>> ListarAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Colegio>>(Colegios.ToList());

    public Task ActualizarAsync(Colegio colegio, CancellationToken cancellationToken)
    {
        Actualizaciones++;
        return Task.CompletedTask;
    }
}

public sealed class RepositorioSupervisoresEnMemoria : IRepositorioSupervisores
{
    public List<Supervisor> Supervisores { get; } = [];

    /// <summary>Simula un fallo de la base de datos al guardar.</summary>
    public bool FallarAlAgregar { get; set; }

    public Task AgregarAsync(Supervisor supervisor, CancellationToken cancellationToken)
    {
        if (FallarAlAgregar) throw new InvalidOperationException("Fallo simulado de base de datos.");
        Supervisores.Add(supervisor);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Supervisor>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Supervisor>>(Supervisores.Where(s => s.ColegioId == colegioId).ToList());
}
