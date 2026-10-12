using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.Servicios;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Application.UseCases.Salida;

/// <summary>Clave con la que se identifica la antena de un colegio (ver <see cref="TokenSeguro"/>).</summary>
public static class ClaveDeAntena
{
    public static string Generar() => TokenSeguro.Generar();

    public static string Hash(string clave) => TokenSeguro.Hash(clave);
}

/// <summary>
/// Regla central del producto: procesa una lectura de tag y decide a quién llamar. Se usa tanto para
/// la antena real como para el simulador de la pantalla de salida.
/// </summary>
public sealed class RegistroDeDetecciones(
    IRepositorioVehiculos vehiculos,
    IRepositorioRepresentantes representantes,
    IRepositorioAlumnos alumnos,
    IRepositorioFranjas franjas,
    IRepositorioDetecciones detecciones,
    IRepositorioRecogidas recogidas,
    TimeProvider reloj)
{
    /// <summary>Mientras el carro avanza en la fila la antena lo lee varias veces: no se vuelve a llamar.</summary>
    public static readonly TimeSpan VentanaAntiDuplicado = TimeSpan.FromMinutes(5);

    public async Task<ResultadoDeteccionDto> RegistrarAsync(Colegio colegio, string tag, bool simulada, CancellationToken cancellationToken)
    {
        var lectura = TagRfid.Crear(tag).Valor;
        var ahora = reloj.GetUtcNow();

        var vehiculo = await vehiculos.ObtenerPorTagAsync(colegio.Id, lectura, cancellationToken);
        if (vehiculo is null)
            return await GuardarAsync(colegio, lectura, null, ahora, ResultadoDeteccion.TagDesconocido, simulada, [], cancellationToken);

        var recientes = await detecciones.ListarDeVehiculoDesdeAsync(vehiculo.Id, ahora - VentanaAntiDuplicado, cancellationToken);
        if (recientes.Any(d => d.Resultado == ResultadoDeteccion.GeneraRecogida))
            return await GuardarAsync(colegio, lectura, vehiculo, ahora, ResultadoDeteccion.Duplicada, simulada, [], cancellationToken);

        var representante = await representantes.ObtenerAsync(vehiculo.RepresentanteId, cancellationToken);
        var hijos = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Where(a => a.Activo && representante is not null && representante.EsRepresentanteDe(a.Id))
            .ToList();
        if (hijos.Count == 0)
            return await GuardarAsync(colegio, lectura, vehiculo, ahora, ResultadoDeteccion.SinAlumnos, simulada, [], cancellationToken);

        // Solo se llama a quien está en su franja de retiro, en la hora local del colegio.
        var horario = new HorarioDeRetiro(await franjas.ListarPorColegioAsync(colegio.Id, cancellationToken));
        var horaLocal = colegio.HoraLocal(ahora);
        var enFranja = hijos.Where(h => horario.EstaEnFranja(h.ClaseId, horaLocal)).ToList();
        if (enFranja.Count == 0)
            return await GuardarAsync(colegio, lectura, vehiculo, ahora, ResultadoDeteccion.FueraDeFranja, simulada, [], cancellationToken);

        // Quien ya está llamado y sin entregar no se vuelve a llamar.
        var yaLlamados = (await recogidas.ListarPendientesAsync(colegio.Id, cancellationToken)).Select(r => r.AlumnoId).ToHashSet();
        var aLlamar = enFranja.Where(h => !yaLlamados.Contains(h.Id)).OrderBy(h => h.Nombres).ToList();
        if (aLlamar.Count == 0)
            return await GuardarAsync(colegio, lectura, vehiculo, ahora, ResultadoDeteccion.Duplicada, simulada, [], cancellationToken);

        return await GuardarAsync(colegio, lectura, vehiculo, ahora, ResultadoDeteccion.GeneraRecogida, simulada, aLlamar, cancellationToken);
    }

    private async Task<ResultadoDeteccionDto> GuardarAsync(
        Colegio colegio, string tag, Vehiculo? vehiculo, DateTimeOffset ahora, ResultadoDeteccion resultado, bool simulada,
        IReadOnlyList<Alumno> aLlamar, CancellationToken cancellationToken)
    {
        var deteccion = Deteccion.Crear(colegio.Id, tag, vehiculo?.Id, ahora, resultado, simulada);
        var nuevas = aLlamar.Select(a => Recogida.Crear(deteccion, a.Id)).ToList();
        await detecciones.GuardarAsync(deteccion, nuevas, cancellationToken);
        return new ResultadoDeteccionDto(resultado, tag, vehiculo?.Placa, aLlamar.Select(a => a.NombreCompleto).ToList());
    }
}

public sealed class RegistrarDeteccionAntenaUseCase(IRepositorioColegios colegios, RegistroDeDetecciones registro) : IRegistrarDeteccionAntenaUseCase
{
    public async Task<ResultadoDeteccionDto> EjecutarAsync(string? claveAntena, string tag, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(claveAntena))
            throw new AccesoDenegadoException("Falta la clave de la antena.");

        var colegio = await colegios.ObtenerPorHashClaveAntenaAsync(ClaveDeAntena.Hash(claveAntena), cancellationToken);
        if (colegio is not { Activo: true })
            throw new AccesoDenegadoException("Clave de antena no válida.");

        return await registro.RegistrarAsync(colegio, tag, simulada: false, cancellationToken);
    }
}

public sealed class SimularLlegadaUseCase(ContextoSupervisor contexto, IRepositorioVehiculos vehiculos, RegistroDeDetecciones registro) : ISimularLlegadaUseCase
{
    public async Task<ResultadoDeteccionDto> EjecutarAsync(string usuarioId, Guid vehiculoId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var vehiculo = await ContextoSupervisor.VehiculoDelColegioAsync(vehiculos, colegio.Id, vehiculoId, cancellationToken);
        if (vehiculo.Tag is null)
            throw new DominioException($"El vehículo {vehiculo.Placa} no tiene tag RFID asignado.");

        return await registro.RegistrarAsync(colegio, vehiculo.Tag, simulada: true, cancellationToken);
    }
}

public sealed class ObtenerPantallaSalidaUseCase(
    ContextoSupervisor contexto,
    IRepositorioRecogidas recogidas,
    IRepositorioAlumnos alumnos,
    IRepositorioClases clases,
    IRepositorioVehiculos vehiculos,
    IRepositorioRepresentantes representantes,
    IRepositorioAutorizados autorizados,
    TimeProvider reloj) : IObtenerPantallaSalidaUseCase
{
    public async Task<PantallaSalidaDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var alumnosPorId = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(a => a.Id);
        var clasesPorId = (await clases.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(c => c.Id, c => c.Nombre);
        var vehiculosDelColegio = await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken);
        var vehiculosPorId = vehiculosDelColegio.ToDictionary(v => v.Id);
        var representantesPorId = (await representantes.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(r => r.Id);

        var autorizadosPorRepresentante = new Dictionary<Guid, IReadOnlyList<PersonaAutorizadaDto>>();
        var pendientes = new List<RecogidaPendienteDto>();
        foreach (var recogida in (await recogidas.ListarPendientesAsync(colegio.Id, cancellationToken)).OrderBy(r => r.DetectadoEn))
        {
            if (!alumnosPorId.TryGetValue(recogida.AlumnoId, out var alumno) || !vehiculosPorId.TryGetValue(recogida.VehiculoId, out var vehiculo))
                continue;
            if (!autorizadosPorRepresentante.TryGetValue(vehiculo.RepresentanteId, out var autorizadas))
            {
                autorizadas = (await autorizados.ListarPorRepresentanteAsync(vehiculo.RepresentanteId, cancellationToken))
                    .Select(a => new PersonaAutorizadaDto(a.Id, a.Nombre, a.Documento, a.Parentesco))
                    .ToList();
                autorizadosPorRepresentante[vehiculo.RepresentanteId] = autorizadas;
            }

            pendientes.Add(new RecogidaPendienteDto(
                recogida.Id,
                alumno.NombreCompleto,
                clasesPorId.GetValueOrDefault(alumno.ClaseId, "—"),
                vehiculo.Placa,
                vehiculo.Descripcion,
                representantesPorId.GetValueOrDefault(vehiculo.RepresentanteId)?.Nombre ?? "—",
                colegio.HoraLocal(recogida.DetectadoEn),
                autorizadas));
        }

        var paraSimular = vehiculosDelColegio
            .Where(v => v.Tag is not null)
            .OrderBy(v => v.Placa)
            .Select(v => new VehiculoParaSimularDto(v.Id, v.Placa, representantesPorId.GetValueOrDefault(v.RepresentanteId)?.Nombre ?? "—"))
            .ToList();

        return new PantallaSalidaDto(colegio.Nombre, colegio.HoraLocal(reloj.GetUtcNow()), pendientes, paraSimular);
    }
}

public sealed class MarcarEntregadaUseCase(ContextoSupervisor contexto, IRepositorioRecogidas recogidas, TimeProvider reloj) : IMarcarEntregadaUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid recogidaId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var recogida = await recogidas.ObtenerAsync(recogidaId, cancellationToken);
        if (recogida is null || recogida.ColegioId != colegio.Id)
            throw new NoEncontradoException("La recogida no existe.");

        recogida.MarcarEntregada(reloj.GetUtcNow());
        await recogidas.ActualizarAsync(recogida, cancellationToken);
    }
}
