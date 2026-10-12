using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioColegiosEf(ApplicationDbContext db) : IRepositorioColegios
{
    public async Task AgregarAsync(Colegio colegio, CancellationToken cancellationToken)
    {
        db.Colegios.Add(colegio);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Colegio?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Colegios.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Colegio>> ListarAsync(CancellationToken cancellationToken) =>
        await db.Colegios.AsNoTracking().ToListAsync(cancellationToken);

    public Task ActualizarAsync(Colegio colegio, CancellationToken cancellationToken)
    {
        db.Colegios.Update(colegio);
        return db.SaveChangesAsync(cancellationToken);
    }

    public Task<Colegio?> ObtenerPorHashClaveAntenaAsync(string hash, CancellationToken cancellationToken) =>
        db.Colegios.AsNoTracking().SingleOrDefaultAsync(c => c.HashClaveAntena == hash, cancellationToken);
}

public sealed class RepositorioSupervisoresEf(ApplicationDbContext db) : IRepositorioSupervisores
{
    public async Task AgregarAsync(Supervisor supervisor, CancellationToken cancellationToken)
    {
        db.Supervisores.Add(supervisor);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Supervisor>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.Supervisores.AsNoTracking().Where(s => s.ColegioId == colegioId).ToListAsync(cancellationToken);

    public Task<Supervisor?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Supervisores.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task ActualizarAsync(Supervisor supervisor, CancellationToken cancellationToken)
    {
        db.Supervisores.Update(supervisor);
        return db.SaveChangesAsync(cancellationToken);
    }

    public Task EliminarAsync(Supervisor supervisor, CancellationToken cancellationToken)
    {
        db.Supervisores.Remove(supervisor);
        return db.SaveChangesAsync(cancellationToken);
    }

    public Task<Supervisor?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
        db.Supervisores.AsNoTracking().SingleOrDefaultAsync(s => s.UsuarioId == usuarioId, cancellationToken);
}
