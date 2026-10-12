using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioDeteccionesEf(ApplicationDbContext db) : IRepositorioDetecciones
{
    // Un único SaveChanges: la detección y sus recogidas se guardan en la misma transacción.
    public async Task GuardarAsync(Deteccion deteccion, IReadOnlyCollection<Recogida> recogidas, CancellationToken cancellationToken)
    {
        db.Detecciones.Add(deteccion);
        db.Recogidas.AddRange(recogidas);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Deteccion>> ListarDeVehiculoDesdeAsync(Guid vehiculoId, DateTimeOffset desde, CancellationToken cancellationToken) =>
        (await db.Detecciones.AsNoTracking().Where(d => d.VehiculoId == vehiculoId).ToListAsync(cancellationToken))
            // SQLite (tests) no traduce comparaciones de DateTimeOffset: se filtra en memoria las de ese vehículo.
            .Where(d => d.DetectadoEn >= desde)
            .ToList();
}

public sealed class RepositorioRecogidasEf(ApplicationDbContext db) : IRepositorioRecogidas
{
    public Task<Recogida?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Recogidas.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Recogida>> ListarPendientesAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.Recogidas.AsNoTracking().Where(r => r.ColegioId == colegioId && r.EntregadoEn == null).ToListAsync(cancellationToken);

    public Task ActualizarAsync(Recogida recogida, CancellationToken cancellationToken)
    {
        db.Recogidas.Update(recogida);
        return db.SaveChangesAsync(cancellationToken);
    }
}
