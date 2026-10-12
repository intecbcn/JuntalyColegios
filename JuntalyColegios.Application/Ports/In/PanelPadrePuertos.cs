using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

// Puertos de entrada del panel de padres. Reciben el id del usuario autenticado: el caso de uso
// resuelve su familia (representante) y limita la operación a ella.

public interface IObtenerPanelPadreUseCase
{
    Task<PanelPadreDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

public interface IRegistrarMiVehiculoUseCase
{
    Task<Guid> EjecutarAsync(string usuarioId, DatosVehiculoDto datos, CancellationToken cancellationToken);
}

public interface IEliminarMiVehiculoUseCase
{
    Task EjecutarAsync(string usuarioId, Guid vehiculoId, CancellationToken cancellationToken);
}

public interface IAgregarAutorizadoUseCase
{
    Task<Guid> EjecutarAsync(string usuarioId, DatosAutorizadoDto datos, CancellationToken cancellationToken);
}

public interface IEliminarAutorizadoUseCase
{
    Task EjecutarAsync(string usuarioId, Guid autorizadoId, CancellationToken cancellationToken);
}
