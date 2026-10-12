using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioPantallasEf(ApplicationDbContext db) : IRepositorioPantallas
{
    public async Task AgregarAsync(PantallaLlamadas pantalla, CancellationToken cancellationToken)
    {
        db.PantallasLlamadas.Add(pantalla);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<PantallaLlamadas?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.PantallasLlamadas.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<PantallaLlamadas?> ObtenerPorHashAsync(string hashToken, CancellationToken cancellationToken) =>
        db.PantallasLlamadas.AsNoTracking().SingleOrDefaultAsync(p => p.HashToken == hashToken, cancellationToken);

    public async Task<IReadOnlyList<PantallaLlamadas>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.PantallasLlamadas.AsNoTracking().Where(p => p.ColegioId == colegioId).ToListAsync(cancellationToken);

    public Task ActualizarAsync(PantallaLlamadas pantalla, CancellationToken cancellationToken)
    {
        if (db.Entry(pantalla).State == EntityState.Detached)
            db.PantallasLlamadas.Update(pantalla);
        return db.SaveChangesAsync(cancellationToken);
    }

    public Task EliminarAsync(PantallaLlamadas pantalla, CancellationToken cancellationToken)
    {
        db.PantallasLlamadas.Remove(pantalla);
        return db.SaveChangesAsync(cancellationToken);
    }
}
