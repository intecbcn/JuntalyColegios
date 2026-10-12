using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.UseCases.Padres;
using JuntalyColegios.Application.UseCases.Supervision;

namespace JuntalyColegios.Application.UseCases.Historial;

/// <summary>Últimas recogidas de los hijos del padre (también de los dados de baja), en hora local del colegio.</summary>
public sealed class ObtenerHistorialPadreUseCase(
    ContextoPadre contexto, IRepositorioAlumnos alumnos, IRepositorioVehiculos vehiculos, IRepositorioRecogidas recogidas)
    : IObtenerHistorialPadreUseCase
{
    public const int Maximo = 20;

    public async Task<IReadOnlyList<HistorialRecogidaDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var (padre, colegio) = await contexto.FamiliaDeAsync(usuarioId, cancellationToken);
        var hijos = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(a => padre.EsRepresentanteDe(a.Id))
            .ToDictionary(a => a.Id, a => a.NombreCompleto);
        if (hijos.Count == 0) return [];

        // El vehículo puede haberse eliminado después: el historial se conserva sin la placa.
        var placas = (await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(v => v.Id, v => v.Placa);

        return (await recogidas.ListarRecientesDeAlumnosAsync(hijos.Keys, Maximo, cancellationToken))
            .Select(r => new HistorialRecogidaDto(
                hijos[r.AlumnoId],
                placas.GetValueOrDefault(r.VehiculoId),
                colegio.HoraLocal(r.DetectadoEn),
                r.EntregadoEn is { } entregado ? colegio.HoraLocal(entregado) : null))
            .ToList();
    }
}

/// <summary>Registro de lecturas de la antena del colegio, para revisar qué pasó a la salida.</summary>
public sealed class ListarDeteccionesUseCase(
    ContextoSupervisor contexto, IRepositorioDetecciones detecciones, IRepositorioVehiculos vehiculos, IRepositorioRepresentantes representantes)
    : IListarDeteccionesUseCase
{
    public const int Maximo = 50;

    public async Task<IReadOnlyList<DeteccionRegistroDto>> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var vehiculosPorId = (await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(v => v.Id);
        var familias = (await representantes.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(r => r.Id, r => r.Nombre);

        return (await detecciones.ListarRecientesAsync(colegio.Id, Maximo, cancellationToken))
            .Select(d =>
            {
                var vehiculo = d.VehiculoId is { } id ? vehiculosPorId.GetValueOrDefault(id) : null;
                return new DeteccionRegistroDto(
                    colegio.HoraLocal(d.DetectadoEn),
                    d.Tag,
                    vehiculo?.Placa,
                    vehiculo is null ? null : familias.GetValueOrDefault(vehiculo.RepresentanteId),
                    d.Resultado,
                    d.Simulada);
            })
            .ToList();
    }
}
