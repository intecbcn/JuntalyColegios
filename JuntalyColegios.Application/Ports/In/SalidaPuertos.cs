using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

/// <summary>Lectura enviada por la antena de un colegio, autenticada con la clave de ese colegio.</summary>
public interface IRegistrarDeteccionAntenaUseCase
{
    /// <summary>Lanza AccesoDenegadoException si la clave no es válida y DominioException si el tag no lo es.</summary>
    Task<ResultadoDeteccionDto> EjecutarAsync(string? claveAntena, string tag, CancellationToken cancellationToken);
}

/// <summary>Simula la llegada de un vehículo del colegio del supervisor (demostración y pruebas).</summary>
public interface ISimularLlegadaUseCase
{
    Task<ResultadoDeteccionDto> EjecutarAsync(string usuarioId, Guid vehiculoId, CancellationToken cancellationToken);
}

public interface IObtenerPantallaSalidaUseCase
{
    Task<PantallaSalidaDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

public interface IMarcarEntregadaUseCase
{
    Task EjecutarAsync(string usuarioId, Guid recogidaId, CancellationToken cancellationToken);
}

/// <summary>Genera una clave de antena nueva para el colegio (invalida la anterior) y la devuelve una única vez.</summary>
public interface IGenerarClaveAntenaUseCase
{
    Task<string> EjecutarAsync(Guid colegioId, CancellationToken cancellationToken);
}
