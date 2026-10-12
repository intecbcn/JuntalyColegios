using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

// Puertos de entrada del área del supervisor. Todos reciben el id del usuario autenticado: el caso de
// uso resuelve su colegio y limita la operación a él.

public interface IListarClasesUseCase
{
    Task<ClasesDelColegioDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

public interface ICrearClaseUseCase
{
    Task<Guid> EjecutarAsync(string usuarioId, DatosClaseDto datos, CancellationToken cancellationToken);
}

public interface IObtenerClaseUseCase
{
    Task<ClaseDetalleDto> EjecutarAsync(string usuarioId, Guid claseId, CancellationToken cancellationToken);
}

public interface IEditarClaseUseCase
{
    Task EjecutarAsync(string usuarioId, Guid claseId, DatosClaseDto datos, CancellationToken cancellationToken);
}

public interface IEliminarClaseUseCase
{
    Task EjecutarAsync(string usuarioId, Guid claseId, CancellationToken cancellationToken);
}

public interface ICrearAlumnoUseCase
{
    Task<Guid> EjecutarAsync(string usuarioId, DatosAlumnoDto datos, CancellationToken cancellationToken);
}

public interface IObtenerAlumnoUseCase
{
    Task<AlumnoDto> EjecutarAsync(string usuarioId, Guid alumnoId, CancellationToken cancellationToken);
}

public interface IEditarAlumnoUseCase
{
    Task EjecutarAsync(string usuarioId, Guid alumnoId, DatosAlumnoDto datos, CancellationToken cancellationToken);
}

public interface ICambiarEstadoAlumnoUseCase
{
    Task EjecutarAsync(string usuarioId, Guid alumnoId, bool activo, CancellationToken cancellationToken);
}
