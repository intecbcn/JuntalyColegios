using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

// Puertos de entrada del supervisor para padres, vínculos con hijos y vehículos/tags.

public interface IListarRepresentantesUseCase
{
    Task<IReadOnlyList<RepresentanteResumenDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

public interface IListarAlumnosParaVincularUseCase
{
    Task<IReadOnlyList<AlumnoParaVincularDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

public interface IAltaRepresentanteUseCase
{
    Task<Guid> EjecutarAsync(string usuarioId, AltaRepresentanteDto alta, CancellationToken cancellationToken);
}

public interface IObtenerRepresentanteUseCase
{
    Task<RepresentanteDetalleDto> EjecutarAsync(string usuarioId, Guid representanteId, CancellationToken cancellationToken);
}

public interface IVincularAlumnoUseCase
{
    Task EjecutarAsync(string usuarioId, Guid representanteId, Guid alumnoId, CancellationToken cancellationToken);
}

public interface IDesvincularAlumnoUseCase
{
    Task EjecutarAsync(string usuarioId, Guid representanteId, Guid alumnoId, CancellationToken cancellationToken);
}

public interface IAgregarVehiculoUseCase
{
    Task<Guid> EjecutarAsync(string usuarioId, Guid representanteId, DatosVehiculoDto datos, CancellationToken cancellationToken);
}

public interface IAsignarTagUseCase
{
    /// <summary>Asigna el tag al vehículo; un tag vacío lo quita.</summary>
    Task EjecutarAsync(string usuarioId, Guid vehiculoId, string? tag, CancellationToken cancellationToken);
}
