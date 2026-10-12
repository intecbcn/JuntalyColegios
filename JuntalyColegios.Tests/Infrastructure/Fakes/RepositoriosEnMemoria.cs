using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Tests.Infrastructure.Fakes;

/// <summary>Repositorio en memoria intercambiable por el de EF Core (Liskov) para tests de casos de uso.</summary>
public sealed class RepositorioColegiosEnMemoria : IRepositorioColegios
{
    public List<Colegio> Colegios { get; } = [];

    public int Actualizaciones { get; private set; }

    public Task AgregarAsync(Colegio colegio, CancellationToken cancellationToken)
    {
        Colegios.Add(colegio);
        return Task.CompletedTask;
    }

    public Task<Colegio?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Colegios.SingleOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Colegio>> ListarAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Colegio>>(Colegios.ToList());

    public Task ActualizarAsync(Colegio colegio, CancellationToken cancellationToken)
    {
        Actualizaciones++;
        return Task.CompletedTask;
    }

    public Task<Colegio?> ObtenerPorHashClaveAntenaAsync(string hash, CancellationToken cancellationToken) =>
        Task.FromResult(Colegios.SingleOrDefault(c => c.HashClaveAntena == hash));
}

public sealed class RepositorioSupervisoresEnMemoria : IRepositorioSupervisores
{
    public List<Supervisor> Supervisores { get; } = [];

    /// <summary>Simula un fallo de la base de datos al guardar.</summary>
    public bool FallarAlAgregar { get; set; }

    public Task AgregarAsync(Supervisor supervisor, CancellationToken cancellationToken)
    {
        if (FallarAlAgregar) throw new InvalidOperationException("Fallo simulado de base de datos.");
        Supervisores.Add(supervisor);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Supervisor>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Supervisor>>(Supervisores.Where(s => s.ColegioId == colegioId).ToList());

    public Task<Supervisor?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
        Task.FromResult(Supervisores.SingleOrDefault(s => s.UsuarioId == usuarioId));
}

public sealed class RepositorioClasesEnMemoria : IRepositorioClases
{
    public List<Clase> Clases { get; } = [];

    public Task AgregarAsync(Clase clase, CancellationToken cancellationToken)
    {
        Clases.Add(clase);
        return Task.CompletedTask;
    }

    public Task<Clase?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Clases.SingleOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Clase>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Clase>>(Clases.Where(c => c.ColegioId == colegioId).ToList());

    public Task ActualizarAsync(Clase clase, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task EliminarAsync(Clase clase, CancellationToken cancellationToken)
    {
        Clases.Remove(clase);
        return Task.CompletedTask;
    }
}

public sealed class RepositorioRepresentantesEnMemoria : IRepositorioRepresentantes
{
    public List<Representante> Representantes { get; } = [];

    public bool FallarAlAgregar { get; set; }

    public Task AgregarAsync(Representante representante, CancellationToken cancellationToken)
    {
        if (FallarAlAgregar) throw new InvalidOperationException("Fallo simulado de base de datos.");
        Representantes.Add(representante);
        return Task.CompletedTask;
    }

    public Task<Representante?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Representantes.SingleOrDefault(r => r.Id == id));

    public Task<Representante?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
        Task.FromResult(Representantes.SingleOrDefault(r => r.UsuarioId == usuarioId));

    public Task<IReadOnlyList<Representante>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Representante>>(Representantes.Where(r => r.ColegioId == colegioId).ToList());

    public Task ActualizarAsync(Representante representante, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class RepositorioVehiculosEnMemoria : IRepositorioVehiculos
{
    public List<Vehiculo> Vehiculos { get; } = [];

    public Task AgregarAsync(Vehiculo vehiculo, CancellationToken cancellationToken)
    {
        Vehiculos.Add(vehiculo);
        return Task.CompletedTask;
    }

    public Task<Vehiculo?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Vehiculos.SingleOrDefault(v => v.Id == id));

    public Task<Vehiculo?> ObtenerPorTagAsync(Guid colegioId, string tag, CancellationToken cancellationToken) =>
        Task.FromResult(Vehiculos.SingleOrDefault(v => v.ColegioId == colegioId && v.Tag == tag));

    public Task<IReadOnlyList<Vehiculo>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Vehiculo>>(Vehiculos.Where(v => v.ColegioId == colegioId).ToList());

    public Task ActualizarAsync(Vehiculo vehiculo, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task EliminarAsync(Vehiculo vehiculo, CancellationToken cancellationToken)
    {
        Vehiculos.Remove(vehiculo);
        return Task.CompletedTask;
    }
}

public sealed class RepositorioAutorizadosEnMemoria : IRepositorioAutorizados
{
    public List<PersonaAutorizada> Autorizados { get; } = [];

    public Task AgregarAsync(PersonaAutorizada autorizada, CancellationToken cancellationToken)
    {
        Autorizados.Add(autorizada);
        return Task.CompletedTask;
    }

    public Task<PersonaAutorizada?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Autorizados.SingleOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<PersonaAutorizada>> ListarPorRepresentanteAsync(Guid representanteId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PersonaAutorizada>>(Autorizados.Where(a => a.RepresentanteId == representanteId).ToList());

    public Task EliminarAsync(PersonaAutorizada autorizada, CancellationToken cancellationToken)
    {
        Autorizados.Remove(autorizada);
        return Task.CompletedTask;
    }
}

public sealed class RepositorioFranjasEnMemoria : IRepositorioFranjas
{
    public List<FranjaRetiro> Franjas { get; } = [];

    public Task AgregarVariasAsync(IReadOnlyCollection<FranjaRetiro> franjas, CancellationToken cancellationToken)
    {
        Franjas.AddRange(franjas);
        return Task.CompletedTask;
    }

    public Task<FranjaRetiro?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Franjas.SingleOrDefault(f => f.Id == id));

    public Task<IReadOnlyList<FranjaRetiro>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FranjaRetiro>>(Franjas.Where(f => f.ColegioId == colegioId).ToList());

    public Task EliminarAsync(FranjaRetiro franja, CancellationToken cancellationToken)
    {
        Franjas.Remove(franja);
        return Task.CompletedTask;
    }
}

public sealed class RepositorioDeteccionesEnMemoria : IRepositorioDetecciones, IRepositorioRecogidas
{
    public List<Deteccion> Detecciones { get; } = [];

    public List<Recogida> Recogidas { get; } = [];

    public Task GuardarAsync(Deteccion deteccion, IReadOnlyCollection<Recogida> recogidas, CancellationToken cancellationToken)
    {
        Detecciones.Add(deteccion);
        Recogidas.AddRange(recogidas);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Deteccion>> ListarDeVehiculoDesdeAsync(Guid vehiculoId, DateTimeOffset desde, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Deteccion>>(Detecciones.Where(d => d.VehiculoId == vehiculoId && d.DetectadoEn >= desde).ToList());

    public Task<Recogida?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Recogidas.SingleOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Recogida>> ListarPendientesAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Recogida>>(Recogidas.Where(r => r.ColegioId == colegioId && r.Pendiente).ToList());

    public Task ActualizarAsync(Recogida recogida, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class RepositorioAlumnosEnMemoria : IRepositorioAlumnos
{
    public List<Alumno> Alumnos { get; } = [];

    public Task AgregarAsync(Alumno alumno, CancellationToken cancellationToken)
    {
        Alumnos.Add(alumno);
        return Task.CompletedTask;
    }

    public Task<Alumno?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Alumnos.SingleOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<Alumno>> ListarPorColegioAsync(Guid colegioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Alumno>>(Alumnos.Where(a => a.ColegioId == colegioId).ToList());

    public Task ActualizarAsync(Alumno alumno, CancellationToken cancellationToken) => Task.CompletedTask;
}
