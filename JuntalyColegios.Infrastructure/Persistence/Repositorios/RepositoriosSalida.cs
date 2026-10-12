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
        await db.Detecciones.AsNoTracking()
            .Where(d => d.VehiculoId == vehiculoId && d.DetectadoEn >= desde)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Deteccion>> ListarRecientesAsync(Guid colegioId, int maximo, CancellationToken cancellationToken) =>
        await db.Detecciones.AsNoTracking()
            .Where(d => d.ColegioId == colegioId)
            .OrderByDescending(d => d.DetectadoEn)
            .Take(maximo)
            .ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyList<Recogida>> ListarRecientesDeAlumnosAsync(
        IReadOnlyCollection<Guid> alumnoIds, int maximo, CancellationToken cancellationToken) =>
        await db.Recogidas.AsNoTracking()
            .Where(r => alumnoIds.Contains(r.AlumnoId))
            .OrderByDescending(r => r.DetectadoEn)
            .Take(maximo)
            .ToListAsync(cancellationToken);
}
