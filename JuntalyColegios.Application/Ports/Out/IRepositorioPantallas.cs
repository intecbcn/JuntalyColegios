using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: pantallas de llamada (portería y aulas) de los colegios.</summary>
public interface IRepositorioPantallas
{
    Task AgregarAsync(PantallaLlamadas pantalla, CancellationToken cancellationToken);

    Task<PantallaLlamadas?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Pantalla cuyo enlace secreto tiene ese hash.</summary>
    Task<PantallaLlamadas?> ObtenerPorHashAsync(string hashToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<PantallaLlamadas>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task ActualizarAsync(PantallaLlamadas pantalla, CancellationToken cancellationToken);

    Task EliminarAsync(PantallaLlamadas pantalla, CancellationToken cancellationToken);
}
