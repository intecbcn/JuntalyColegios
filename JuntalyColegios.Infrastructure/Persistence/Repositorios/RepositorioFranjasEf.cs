using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioFranjasEf(ApplicationDbContext db) : IRepositorioFranjas
{
    // Un único SaveChanges: EF lo ejecuta en una transacción (todas o ninguna).
    public async Task AgregarVariasAsync(IReadOnlyCollection<FranjaRetiro> franjas, CancellationToken cancellationToken)
    {
        db.FranjasRetiro.AddRange(franjas);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<FranjaRetiro?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.FranjasRetiro.SingleOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FranjaRetiro>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.FranjasRetiro.AsNoTracking().Where(f => f.ColegioId == colegioId).ToListAsync(cancellationToken);

    public Task EliminarAsync(FranjaRetiro franja, CancellationToken cancellationToken)
    {
        db.FranjasRetiro.Remove(franja);
        return db.SaveChangesAsync(cancellationToken);
    }
}
