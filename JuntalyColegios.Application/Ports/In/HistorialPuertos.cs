using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

/// <summary>Últimas recogidas de los hijos del padre autenticado.</summary>
public interface IObtenerHistorialPadreUseCase
{
    Task<IReadOnlyList<HistorialRecogidaDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

/// <summary>Últimas lecturas de la antena del colegio del supervisor, generen o no llamadas.</summary>
public interface IListarDeteccionesUseCase
{
    Task<IReadOnlyList<DeteccionRegistroDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}
