using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

// Puertos de entrada de la gestión de colegios (área Admin): uno por operación de negocio.

public interface ICrearColegioUseCase
{
    Task<Guid> EjecutarAsync(DatosColegioDto datos, CancellationToken cancellationToken);
}

public interface IListarColegiosUseCase
{
    Task<IReadOnlyList<ColegioDto>> EjecutarAsync(CancellationToken cancellationToken);
}

public interface IObtenerColegioUseCase
{
    /// <summary>Lanza NoEncontradoException si el colegio no existe.</summary>
    Task<ColegioDetalleDto> EjecutarAsync(Guid id, CancellationToken cancellationToken);
}

public interface IEditarColegioUseCase
{
    Task EjecutarAsync(Guid id, DatosColegioDto datos, CancellationToken cancellationToken);
}

public interface ICambiarEstadoColegioUseCase
{
    Task EjecutarAsync(Guid id, bool activo, CancellationToken cancellationToken);
}

public interface IAltaSupervisorUseCase
{
    Task<Guid> EjecutarAsync(AltaSupervisorDto alta, CancellationToken cancellationToken);
}
