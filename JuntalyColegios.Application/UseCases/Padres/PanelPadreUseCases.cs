using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.UseCases.Vehiculos;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Application.UseCases.Padres;

/// <summary>
/// Resuelve la familia (representante) del padre autenticado. Lo de otra familia se trata como
/// inexistente (NoEncontrado), para no revelar que existe.
/// </summary>
public sealed class ContextoPadre(IRepositorioRepresentantes representantes, IRepositorioColegios colegios)
{
    public async Task<(Representante Representante, Colegio Colegio)> FamiliaDeAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var representante = await representantes.ObtenerPorUsuarioAsync(usuarioId, cancellationToken)
            ?? throw new AccesoDenegadoException("El usuario no está registrado como padre o representante.");
        var colegio = await colegios.ObtenerAsync(representante.ColegioId, cancellationToken);
        if (colegio is not { Activo: true })
            throw new AccesoDenegadoException("El colegio está desactivado.");
        return (representante, colegio);
    }

    public async Task<Representante> RepresentanteDeAsync(string usuarioId, CancellationToken cancellationToken) =>
        (await FamiliaDeAsync(usuarioId, cancellationToken)).Representante;
}

public sealed class ObtenerPanelPadreUseCase(
    ContextoPadre contexto,
    IRepositorioAlumnos alumnos,
    IRepositorioClases clases,
    IRepositorioVehiculos vehiculos,
    IRepositorioAutorizados autorizados) : IObtenerPanelPadreUseCase
{
    public async Task<PanelPadreDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var (padre, colegio) = await contexto.FamiliaDeAsync(usuarioId, cancellationToken);

        var nombresDeClase = (await clases.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(c => c.Id, c => c.Nombre);
        var hijos = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(a => a.Activo && padre.EsRepresentanteDe(a.Id))
            .OrderBy(a => a.Nombres, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => new HijoDto(a.Id, a.NombreCompleto, nombresDeClase.GetValueOrDefault(a.ClaseId, "—")))
            .ToList();
        var suyos = (await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(v => v.RepresentanteId == padre.Id)
            .OrderBy(v => v.Placa)
            .Select(v => new VehiculoDto(v.Id, v.Placa, v.Descripcion, v.Tag))
            .ToList();
        var autorizadas = (await autorizados.ListarPorRepresentanteAsync(padre.Id, cancellationToken))
            .OrderBy(a => a.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => new PersonaAutorizadaDto(a.Id, a.Nombre, a.Documento, a.Parentesco))
            .ToList();

        return new PanelPadreDto(padre.Nombre, colegio.Nombre, hijos, suyos, autorizadas);
    }
}

public sealed class RegistrarMiVehiculoUseCase(ContextoPadre contexto, IRepositorioVehiculos vehiculos) : IRegistrarMiVehiculoUseCase
{
    public async Task<Guid> EjecutarAsync(string usuarioId, DatosVehiculoDto datos, CancellationToken cancellationToken)
    {
        var padre = await contexto.RepresentanteDeAsync(usuarioId, cancellationToken);
        // El padre registra el vehículo; el tag lo asigna el colegio al instalarlo.
        return await RegistroDeVehiculos.RegistrarAsync(vehiculos, padre, datos, cancellationToken);
    }
}

public sealed class EliminarMiVehiculoUseCase(ContextoPadre contexto, IRepositorioVehiculos vehiculos) : IEliminarMiVehiculoUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid vehiculoId, CancellationToken cancellationToken)
    {
        var padre = await contexto.RepresentanteDeAsync(usuarioId, cancellationToken);
        var vehiculo = await vehiculos.ObtenerAsync(vehiculoId, cancellationToken);
        if (vehiculo is null || vehiculo.RepresentanteId != padre.Id)
            throw new NoEncontradoException("El vehículo no existe.");
        await vehiculos.EliminarAsync(vehiculo, cancellationToken);
    }
}

public sealed class AgregarAutorizadoUseCase(ContextoPadre contexto, IRepositorioAutorizados autorizados) : IAgregarAutorizadoUseCase
{
    public async Task<Guid> EjecutarAsync(string usuarioId, DatosAutorizadoDto datos, CancellationToken cancellationToken)
    {
        var padre = await contexto.RepresentanteDeAsync(usuarioId, cancellationToken);
        var autorizada = PersonaAutorizada.Crear(padre, datos.Nombre, datos.Documento, datos.Parentesco);

        var existentes = await autorizados.ListarPorRepresentanteAsync(padre.Id, cancellationToken);
        if (existentes.Any(a => a.TieneDocumento(autorizada.Documento)))
            throw new DominioException($"Ya has autorizado a una persona con el documento {autorizada.Documento}.");

        await autorizados.AgregarAsync(autorizada, cancellationToken);
        return autorizada.Id;
    }
}

public sealed class EliminarAutorizadoUseCase(ContextoPadre contexto, IRepositorioAutorizados autorizados) : IEliminarAutorizadoUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid autorizadoId, CancellationToken cancellationToken)
    {
        var padre = await contexto.RepresentanteDeAsync(usuarioId, cancellationToken);
        var autorizada = await autorizados.ObtenerAsync(autorizadoId, cancellationToken);
        if (autorizada is null || autorizada.RepresentanteId != padre.Id)
            throw new NoEncontradoException("La persona autorizada no existe.");
        await autorizados.EliminarAsync(autorizada, cancellationToken);
    }
}
