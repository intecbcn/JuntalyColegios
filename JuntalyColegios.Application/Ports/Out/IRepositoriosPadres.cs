using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: persistencia de padres/representantes (con sus vínculos a alumnos).</summary>
public interface IRepositorioRepresentantes
{
    Task AgregarAsync(Representante representante, CancellationToken cancellationToken);

    Task<Representante?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<Representante?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Representante>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task ActualizarAsync(Representante representante, CancellationToken cancellationToken);
}

/// <summary>Puerto de salida: persistencia de vehículos.</summary>
public interface IRepositorioVehiculos
{
    Task AgregarAsync(Vehiculo vehiculo, CancellationToken cancellationToken);

    Task<Vehiculo?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Vehículo del colegio con ese tag (normalizado), si lo hay.</summary>
    Task<Vehiculo?> ObtenerPorTagAsync(Guid colegioId, string tag, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vehiculo>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken);

    Task ActualizarAsync(Vehiculo vehiculo, CancellationToken cancellationToken);

    Task EliminarAsync(Vehiculo vehiculo, CancellationToken cancellationToken);
}
