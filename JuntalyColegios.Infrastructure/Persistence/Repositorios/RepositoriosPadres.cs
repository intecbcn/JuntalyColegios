using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioRepresentantesEf(ApplicationDbContext db) : IRepositorioRepresentantes
{
    public async Task AgregarAsync(Representante representante, CancellationToken cancellationToken)
    {
        db.Representantes.Add(representante);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Los vínculos con alumnos son parte del agregado (owned): EF los carga siempre con el representante.
    public Task<Representante?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Representantes.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Representante?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
        db.Representantes.SingleOrDefaultAsync(r => r.UsuarioId == usuarioId, cancellationToken);

    public async Task<IReadOnlyList<Representante>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.Representantes.AsNoTracking().Where(r => r.ColegioId == colegioId).ToListAsync(cancellationToken);

    // Si llega rastreado (desde ObtenerAsync), basta con guardar: EF detecta los vínculos añadidos o
    // quitados. Update() sobre una entidad rastreada marcaría los vínculos nuevos como modificados.
    public Task ActualizarAsync(Representante representante, CancellationToken cancellationToken)
    {
        if (db.Entry(representante).State == EntityState.Detached)
            db.Representantes.Update(representante);
        return db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RepositorioVehiculosEf(ApplicationDbContext db) : IRepositorioVehiculos
{
    public async Task AgregarAsync(Vehiculo vehiculo, CancellationToken cancellationToken)
    {
        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Vehiculo?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Vehiculos.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehiculo?> ObtenerPorTagAsync(Guid colegioId, string tag, CancellationToken cancellationToken) =>
        db.Vehiculos.AsNoTracking().SingleOrDefaultAsync(v => v.ColegioId == colegioId && v.Tag == tag, cancellationToken);

    public async Task<IReadOnlyList<Vehiculo>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.Vehiculos.AsNoTracking().Where(v => v.ColegioId == colegioId).ToListAsync(cancellationToken);

    public Task ActualizarAsync(Vehiculo vehiculo, CancellationToken cancellationToken)
    {
        db.Vehiculos.Update(vehiculo);
        return db.SaveChangesAsync(cancellationToken);
    }

    public Task EliminarAsync(Vehiculo vehiculo, CancellationToken cancellationToken)
    {
        db.Vehiculos.Remove(vehiculo);
        return db.SaveChangesAsync(cancellationToken);
    }
}
