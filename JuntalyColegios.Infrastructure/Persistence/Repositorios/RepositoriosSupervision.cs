using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioClasesEf(ApplicationDbContext db) : IRepositorioClases
{
    public async Task AgregarAsync(Clase clase, CancellationToken cancellationToken)
    {
        db.Clases.Add(clase);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Clase?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Clases.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Clase>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.Clases.AsNoTracking().Where(c => c.ColegioId == colegioId).ToListAsync(cancellationToken);

    public Task ActualizarAsync(Clase clase, CancellationToken cancellationToken)
    {
        db.Clases.Update(clase);
        return db.SaveChangesAsync(cancellationToken);
    }

    public Task EliminarAsync(Clase clase, CancellationToken cancellationToken)
    {
        db.Clases.Remove(clase);
        return db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RepositorioAlumnosEf(ApplicationDbContext db) : IRepositorioAlumnos
{
    public async Task AgregarAsync(Alumno alumno, CancellationToken cancellationToken)
    {
        db.Alumnos.Add(alumno);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Alumno?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Alumnos.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Alumno>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        await db.Alumnos.AsNoTracking().Where(a => a.ColegioId == colegioId).ToListAsync(cancellationToken);

    public Task ActualizarAsync(Alumno alumno, CancellationToken cancellationToken)
    {
        db.Alumnos.Update(alumno);
        return db.SaveChangesAsync(cancellationToken);
    }
}
