using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Vehiculos;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Application.UseCases.Supervision;

// Casos de uso del supervisor para padres, sus hijos y los tags RFID de sus vehículos. Todo queda
// limitado al colegio del supervisor (ContextoSupervisor).

internal static class PadresConsultas
{
    /// <summary>Alumnos del colegio con el nombre de su clase, para mostrarlos a quien vincula.</summary>
    public static async Task<IReadOnlyList<AlumnoParaVincularDto>> AlumnosConClaseAsync(
        IRepositorioAlumnos alumnos, IRepositorioClases clases, Guid colegioId, Func<Alumno, bool> filtro, CancellationToken ct)
    {
        var nombresDeClase = (await clases.ListarPorColegioAsync(colegioId, ct)).ToDictionary(c => c.Id, c => c.Nombre);
        return (await alumnos.ListarPorColegioAsync(colegioId, ct))
            .Where(filtro)
            .OrderBy(a => a.Nombres, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(a => a.Apellidos, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => new AlumnoParaVincularDto(a.Id, a.NombreCompleto, nombresDeClase.GetValueOrDefault(a.ClaseId, "—")))
            .ToList();
    }
}

public sealed class ListarRepresentantesUseCase(
    ContextoSupervisor contexto, IRepositorioRepresentantes representantes, IRepositorioVehiculos vehiculos) : IListarRepresentantesUseCase
{
    public async Task<IReadOnlyList<RepresentanteResumenDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var vehiculosPorRepresentante = (await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .GroupBy(v => v.RepresentanteId)
            .ToDictionary(g => g.Key, g => g.Count());

        return (await representantes.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .OrderBy(r => r.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new RepresentanteResumenDto(
                r.Id, r.Nombre, r.Email, r.Telefono, r.Alumnos.Count, vehiculosPorRepresentante.GetValueOrDefault(r.Id)))
            .ToList();
    }
}

public sealed class ListarAlumnosParaVincularUseCase(
    ContextoSupervisor contexto, IRepositorioAlumnos alumnos, IRepositorioClases clases) : IListarAlumnosParaVincularUseCase
{
    public async Task<IReadOnlyList<AlumnoParaVincularDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        return await PadresConsultas.AlumnosConClaseAsync(alumnos, clases, colegio.Id, a => a.Activo, cancellationToken);
    }
}

public sealed class AltaRepresentanteUseCase(
    ContextoSupervisor contexto, IRepositorioAlumnos alumnos, IRepositorioRepresentantes representantes, IServicioUsuarios usuarios)
    : IAltaRepresentanteUseCase
{
    public async Task<Guid> EjecutarAsync(string usuarioId, AltaRepresentanteDto alta, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);

        // Todo se valida antes de crear la cuenta, para no dejar cuentas huérfanas por datos inválidos.
        var nombre = Representante.ValidarNombre(alta.Nombre);
        var telefono = Representante.ValidarTelefono(alta.Telefono);
        var email = Email.Crear(alta.Email);
        var hijos = new List<Alumno>();
        foreach (var alumnoId in alta.AlumnoIds.Distinct())
            hijos.Add(await ContextoSupervisor.AlumnoDelColegioAsync(alumnos, colegio.Id, alumnoId, cancellationToken));

        var cuentaId = await usuarios.CrearUsuarioInvitadoAsync(email.Valor, nombre, Roles.Padre, cancellationToken);
        try
        {
            var representante = Representante.Crear(colegio.Id, cuentaId, nombre, email, telefono);
            foreach (var hijo in hijos) representante.VincularAlumno(hijo);
            await representantes.AgregarAsync(representante, cancellationToken);
            return representante.Id;
        }
        catch
        {
            // Sin el representante la cuenta quedaría huérfana y bloquearía reintentar el alta.
            await usuarios.EliminarUsuarioAsync(cuentaId, CancellationToken.None);
            throw;
        }
    }
}

public sealed class ObtenerRepresentanteUseCase(
    ContextoSupervisor contexto,
    IRepositorioRepresentantes representantes,
    IRepositorioAlumnos alumnos,
    IRepositorioClases clases,
    IRepositorioVehiculos vehiculos) : IObtenerRepresentanteUseCase
{
    public async Task<RepresentanteDetalleDto> EjecutarAsync(string usuarioId, Guid representanteId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var representante = await ContextoSupervisor.RepresentanteDelColegioAsync(representantes, colegio.Id, representanteId, cancellationToken);

        var hijos = await PadresConsultas.AlumnosConClaseAsync(
            alumnos, clases, colegio.Id, a => representante.EsRepresentanteDe(a.Id), cancellationToken);
        var porVincular = await PadresConsultas.AlumnosConClaseAsync(
            alumnos, clases, colegio.Id, a => a.Activo && !representante.EsRepresentanteDe(a.Id), cancellationToken);
        var suyos = (await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(v => v.RepresentanteId == representante.Id)
            .OrderBy(v => v.Placa)
            .Select(v => new VehiculoDto(v.Id, v.Placa, v.Descripcion, v.Tag))
            .ToList();

        return new RepresentanteDetalleDto(
            representante.Id, representante.Nombre, representante.Email, representante.Telefono,
            hijos.Select(h => new HijoDto(h.AlumnoId, h.NombreCompleto, h.Clase)).ToList(),
            suyos,
            porVincular);
    }
}

public sealed class VincularAlumnoUseCase(
    ContextoSupervisor contexto, IRepositorioRepresentantes representantes, IRepositorioAlumnos alumnos) : IVincularAlumnoUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid representanteId, Guid alumnoId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var representante = await ContextoSupervisor.RepresentanteDelColegioAsync(representantes, colegio.Id, representanteId, cancellationToken);
        var alumno = await ContextoSupervisor.AlumnoDelColegioAsync(alumnos, colegio.Id, alumnoId, cancellationToken);

        representante.VincularAlumno(alumno);
        await representantes.ActualizarAsync(representante, cancellationToken);
    }
}

public sealed class DesvincularAlumnoUseCase(ContextoSupervisor contexto, IRepositorioRepresentantes representantes) : IDesvincularAlumnoUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid representanteId, Guid alumnoId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var representante = await ContextoSupervisor.RepresentanteDelColegioAsync(representantes, colegio.Id, representanteId, cancellationToken);

        representante.DesvincularAlumno(alumnoId);
        await representantes.ActualizarAsync(representante, cancellationToken);
    }
}

public sealed class AgregarVehiculoUseCase(
    ContextoSupervisor contexto, IRepositorioRepresentantes representantes, IRepositorioVehiculos vehiculos) : IAgregarVehiculoUseCase
{
    public async Task<Guid> EjecutarAsync(string usuarioId, Guid representanteId, DatosVehiculoDto datos, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var representante = await ContextoSupervisor.RepresentanteDelColegioAsync(representantes, colegio.Id, representanteId, cancellationToken);
        return await RegistroDeVehiculos.RegistrarAsync(vehiculos, representante, datos, cancellationToken);
    }
}

public sealed class AsignarTagUseCase(ContextoSupervisor contexto, IRepositorioVehiculos vehiculos) : IAsignarTagUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid vehiculoId, string? tag, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var vehiculo = await ContextoSupervisor.VehiculoDelColegioAsync(vehiculos, colegio.Id, vehiculoId, cancellationToken);

        if (string.IsNullOrWhiteSpace(tag))
        {
            vehiculo.QuitarTag();
        }
        else
        {
            // La antena identifica el vehículo por el tag: no puede haber dos con el mismo en el colegio.
            var nuevo = TagRfid.Crear(tag);
            var otro = await vehiculos.ObtenerPorTagAsync(colegio.Id, nuevo.Valor, cancellationToken);
            if (otro is not null && otro.Id != vehiculo.Id)
                throw new DominioException($"El tag {nuevo.Valor} ya está asignado al vehículo {otro.Placa}.");
            vehiculo.AsignarTag(nuevo);
        }
        await vehiculos.ActualizarAsync(vehiculo, cancellationToken);
    }
}
