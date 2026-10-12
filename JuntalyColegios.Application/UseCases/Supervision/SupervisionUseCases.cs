using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Application.UseCases.Supervision;

/// <summary>
/// Resuelve el colegio del supervisor autenticado y obtiene clases/alumnos solo si son de ese colegio.
/// Es el único punto donde se decide el aislamiento entre colegios: lo de otro colegio se trata como
/// inexistente (NoEncontrado), para no revelar que existe.
/// </summary>
public sealed class ContextoSupervisor(IRepositorioSupervisores supervisores, IRepositorioColegios colegios)
{
    public async Task<Colegio> ColegioDeAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var supervisor = await supervisores.ObtenerPorUsuarioAsync(usuarioId, cancellationToken)
            ?? throw new AccesoDenegadoException("El usuario no es supervisor de ningún colegio.");
        var colegio = await colegios.ObtenerAsync(supervisor.ColegioId, cancellationToken);
        if (colegio is not { Activo: true })
            throw new AccesoDenegadoException("El colegio del supervisor está desactivado.");
        return colegio;
    }

    public static async Task<Clase> ClaseDelColegioAsync(
        IRepositorioClases clases, Guid colegioId, Guid claseId, CancellationToken cancellationToken)
    {
        var clase = await clases.ObtenerAsync(claseId, cancellationToken);
        return clase is not null && clase.ColegioId == colegioId
            ? clase
            : throw new NoEncontradoException("La clase no existe.");
    }

    public static async Task<Alumno> AlumnoDelColegioAsync(
        IRepositorioAlumnos alumnos, Guid colegioId, Guid alumnoId, CancellationToken cancellationToken)
    {
        var alumno = await alumnos.ObtenerAsync(alumnoId, cancellationToken);
        return alumno is not null && alumno.ColegioId == colegioId
            ? alumno
            : throw new NoEncontradoException("El alumno no existe.");
    }

    public static async Task<Representante> RepresentanteDelColegioAsync(
        IRepositorioRepresentantes representantes, Guid colegioId, Guid representanteId, CancellationToken cancellationToken)
    {
        var representante = await representantes.ObtenerAsync(representanteId, cancellationToken);
        return representante is not null && representante.ColegioId == colegioId
            ? representante
            : throw new NoEncontradoException("El representante no existe.");
    }

    public static async Task<Vehiculo> VehiculoDelColegioAsync(
        IRepositorioVehiculos vehiculos, Guid colegioId, Guid vehiculoId, CancellationToken cancellationToken)
    {
        var vehiculo = await vehiculos.ObtenerAsync(vehiculoId, cancellationToken);
        return vehiculo is not null && vehiculo.ColegioId == colegioId
            ? vehiculo
            : throw new NoEncontradoException("El vehículo no existe.");
    }

    /// <summary>Los nombres de clase no se repiten dentro de un colegio.</summary>
    public static async Task ValidarNombreLibreAsync(
        IRepositorioClases clases, Guid colegioId, string nombre, Guid? claseEditada, CancellationToken cancellationToken)
    {
        var existentes = await clases.ListarPorColegioAsync(colegioId, cancellationToken);
        if (existentes.Any(c => c.Id != claseEditada && c.TieneNombre(nombre)))
            throw new DominioException($"Ya existe una clase llamada «{nombre.Trim()}» en el colegio.");
    }
}

internal static class SupervisionMapeo
{
    public static AlumnoDto ADto(this Alumno a) => new(a.Id, a.ClaseId, a.Nombres, a.Apellidos, a.Activo);
}

public sealed class ListarClasesUseCase(ContextoSupervisor contexto, IRepositorioClases clases, IRepositorioAlumnos alumnos) : IListarClasesUseCase
{
    public async Task<ClasesDelColegioDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var alumnosPorClase = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(a => a.Activo)
            .GroupBy(a => a.ClaseId)
            .ToDictionary(g => g.Key, g => g.Count());

        var resumen = (await clases.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => new ClaseResumenDto(c.Id, c.Nombre, c.Nivel, alumnosPorClase.GetValueOrDefault(c.Id)))
            .ToList();
        return new ClasesDelColegioDto(colegio.Nombre, resumen);
    }
}

public sealed class CrearClaseUseCase(ContextoSupervisor contexto, IRepositorioClases clases) : ICrearClaseUseCase
{
    public async Task<Guid> EjecutarAsync(string usuarioId, DatosClaseDto datos, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var clase = Clase.Crear(colegio.Id, datos.Nombre, datos.Nivel);
        await ContextoSupervisor.ValidarNombreLibreAsync(clases, colegio.Id, clase.Nombre, null, cancellationToken);
        await clases.AgregarAsync(clase, cancellationToken);
        return clase.Id;
    }
}

public sealed class ObtenerClaseUseCase(ContextoSupervisor contexto, IRepositorioClases clases, IRepositorioAlumnos alumnos) : IObtenerClaseUseCase
{
    public async Task<ClaseDetalleDto> EjecutarAsync(string usuarioId, Guid claseId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var clase = await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, claseId, cancellationToken);
        var deLaClase = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(a => a.ClaseId == clase.Id)
            .OrderBy(a => a.Apellidos, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(a => a.Nombres, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => a.ADto())
            .ToList();
        return new ClaseDetalleDto(clase.Id, clase.Nombre, clase.Nivel, deLaClase);
    }
}

public sealed class EditarClaseUseCase(ContextoSupervisor contexto, IRepositorioClases clases) : IEditarClaseUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid claseId, DatosClaseDto datos, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var clase = await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, claseId, cancellationToken);
        await ContextoSupervisor.ValidarNombreLibreAsync(clases, colegio.Id, datos.Nombre, clase.Id, cancellationToken);
        clase.Actualizar(datos.Nombre, datos.Nivel);
        await clases.ActualizarAsync(clase, cancellationToken);
    }
}

public sealed class EliminarClaseUseCase(ContextoSupervisor contexto, IRepositorioClases clases, IRepositorioAlumnos alumnos) : IEliminarClaseUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid claseId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var clase = await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, claseId, cancellationToken);

        // También cuentan los dados de baja: conservan su historial asociado a la clase.
        if ((await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken)).Any(a => a.ClaseId == clase.Id))
            throw new DominioException("No se puede eliminar una clase con alumnos. Cámbialos de clase primero.");

        await clases.EliminarAsync(clase, cancellationToken);
    }
}

public sealed class CrearAlumnoUseCase(ContextoSupervisor contexto, IRepositorioClases clases, IRepositorioAlumnos alumnos) : ICrearAlumnoUseCase
{
    public async Task<Guid> EjecutarAsync(string usuarioId, DatosAlumnoDto datos, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var clase = await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, datos.ClaseId, cancellationToken);
        var alumno = Alumno.Crear(clase, datos.Nombres, datos.Apellidos);
        await alumnos.AgregarAsync(alumno, cancellationToken);
        return alumno.Id;
    }
}

public sealed class ObtenerAlumnoUseCase(ContextoSupervisor contexto, IRepositorioAlumnos alumnos) : IObtenerAlumnoUseCase
{
    public async Task<AlumnoDto> EjecutarAsync(string usuarioId, Guid alumnoId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        return (await ContextoSupervisor.AlumnoDelColegioAsync(alumnos, colegio.Id, alumnoId, cancellationToken)).ADto();
    }
}

public sealed class EditarAlumnoUseCase(ContextoSupervisor contexto, IRepositorioClases clases, IRepositorioAlumnos alumnos) : IEditarAlumnoUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid alumnoId, DatosAlumnoDto datos, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var alumno = await ContextoSupervisor.AlumnoDelColegioAsync(alumnos, colegio.Id, alumnoId, cancellationToken);
        var clase = await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, datos.ClaseId, cancellationToken);

        alumno.Actualizar(datos.Nombres, datos.Apellidos);
        alumno.CambiarDeClase(clase);
        await alumnos.ActualizarAsync(alumno, cancellationToken);
    }
}

public sealed class CambiarEstadoAlumnoUseCase(ContextoSupervisor contexto, IRepositorioAlumnos alumnos) : ICambiarEstadoAlumnoUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid alumnoId, bool activo, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var alumno = await ContextoSupervisor.AlumnoDelColegioAsync(alumnos, colegio.Id, alumnoId, cancellationToken);
        if (activo) alumno.Reactivar();
        else alumno.DarDeBaja();
        await alumnos.ActualizarAsync(alumno, cancellationToken);
    }
}
