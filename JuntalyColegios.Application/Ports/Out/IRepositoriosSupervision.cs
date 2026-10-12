using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: persistencia de clases.</summary>
public interface IRepositorioClases
{
    Task AgregarAsync(Clase clase, CancellationToken cancellationToken);

    Task<Clase?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Clase>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task ActualizarAsync(Clase clase, CancellationToken cancellationToken);

    Task EliminarAsync(Clase clase, CancellationToken cancellationToken);
}

/// <summary>Puerto de salida: persistencia de alumnos.</summary>
public interface IRepositorioAlumnos
{
    Task AgregarAsync(Alumno alumno, CancellationToken cancellationToken);

    Task<Alumno?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Alumno>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task ActualizarAsync(Alumno alumno, CancellationToken cancellationToken);
}
