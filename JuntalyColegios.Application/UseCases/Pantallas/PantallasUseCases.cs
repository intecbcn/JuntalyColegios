using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.UseCases.Pantallas;

// Pantallas de llamada (TV de portería y tablets de aula). El supervisor las gestiona; la pantalla se
// abre con un enlace secreto y solo muestra lo imprescindible para preparar al alumno.

internal static class NombresDePantalla
{
    public const string Porteria = "Portería";

    public static string De(PantallaLlamadas pantalla, IReadOnlyDictionary<Guid, string> clases) =>
        pantalla.ClaseId is { } claseId ? $"Aula {clases.GetValueOrDefault(claseId, "—")}" : Porteria;
}

public sealed class ListarPantallasUseCase(ContextoSupervisor contexto, IRepositorioPantallas pantallas, IRepositorioClases clases) : IListarPantallasUseCase
{
    public async Task<PantallasDelColegioDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var opciones = (await clases.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => new OpcionDto(c.Id, c.Nombre))
            .ToList();
        var nombres = opciones.ToDictionary(c => c.Id, c => c.Nombre);

        var lista = (await pantallas.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .Select(p => new PantallaDto(p.Id, NombresDePantalla.De(p, nombres), p.ClaseId))
            .OrderBy(p => p.ClaseId is null ? 0 : 1)
            .ThenBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new PantallasDelColegioDto(lista, opciones);
    }
}

public sealed class CrearPantallaUseCase(ContextoSupervisor contexto, IRepositorioPantallas pantallas, IRepositorioClases clases) : ICrearPantallaUseCase
{
    public async Task<string> EjecutarAsync(string usuarioId, Guid? claseId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        if (claseId is { } id)
            await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, id, cancellationToken);

        var token = TokenSeguro.Generar();
        var hash = TokenSeguro.Hash(token);

        // Una pantalla por ámbito: si ya existe se regenera su enlace (el anterior deja de funcionar).
        var existente = (await pantallas.ListarPorColegioAsync(colegio.Id, cancellationToken)).FirstOrDefault(p => p.ClaseId == claseId);
        if (existente is null)
        {
            await pantallas.AgregarAsync(PantallaLlamadas.Crear(colegio.Id, claseId, hash), cancellationToken);
        }
        else
        {
            var pantalla = await pantallas.ObtenerAsync(existente.Id, cancellationToken) ?? existente;
            pantalla.RegenerarToken(hash);
            await pantallas.ActualizarAsync(pantalla, cancellationToken);
        }
        return token;
    }
}

public sealed class RevocarPantallaUseCase(ContextoSupervisor contexto, IRepositorioPantallas pantallas) : IRevocarPantallaUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid pantallaId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var pantalla = await pantallas.ObtenerAsync(pantallaId, cancellationToken);
        if (pantalla is null || pantalla.ColegioId != colegio.Id)
            throw new NoEncontradoException("La pantalla no existe.");
        await pantallas.EliminarAsync(pantalla, cancellationToken);
    }
}

public sealed class ObtenerPantallaPublicaUseCase(
    IRepositorioPantallas pantallas,
    IRepositorioColegios colegios,
    IRepositorioClases clases,
    IRepositorioAlumnos alumnos,
    IRepositorioVehiculos vehiculos,
    IRepositorioRecogidas recogidas,
    TimeProvider reloj) : IObtenerPantallaPublicaUseCase
{
    public async Task<PantallaPublicaDto> EjecutarAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new AccesoDenegadoException("Falta el enlace de la pantalla.");
        var pantalla = await pantallas.ObtenerPorHashAsync(TokenSeguro.Hash(token), cancellationToken)
            ?? throw new AccesoDenegadoException("Enlace de pantalla no válido.");
        var colegio = await colegios.ObtenerAsync(pantalla.ColegioId, cancellationToken);
        if (colegio is not { Activo: true })
            throw new AccesoDenegadoException("El colegio está desactivado.");

        var nombresDeClase = (await clases.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(c => c.Id, c => c.Nombre);
        var alumnosPorId = (await alumnos.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(a => a.Id);
        var placas = (await vehiculos.ListarPorColegioAsync(colegio.Id, cancellationToken)).ToDictionary(v => v.Id, v => v.Placa);

        var llamados = (await recogidas.ListarPendientesAsync(colegio.Id, cancellationToken))
            .Where(r => alumnosPorId.TryGetValue(r.AlumnoId, out var a) && (pantalla.EsPorteria || a.ClaseId == pantalla.ClaseId))
            .OrderByDescending(r => r.DetectadoEn)
            .Select(r =>
            {
                var alumno = alumnosPorId[r.AlumnoId];
                return new LlamadoDto(
                    r.Id, alumno.NombreCompleto, nombresDeClase.GetValueOrDefault(alumno.ClaseId, "—"),
                    placas.GetValueOrDefault(r.VehiculoId), colegio.HoraLocal(r.DetectadoEn));
            })
            .ToList();

        return new PantallaPublicaDto(
            colegio.Nombre,
            NombresDePantalla.De(pantalla, nombresDeClase),
            colegio.HoraLocal(reloj.GetUtcNow()),
            llamados.FirstOrDefault(),
            llamados.Skip(1).ToList());
    }
}
