using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Formato;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.Servicios;

namespace JuntalyColegios.Application.UseCases.Supervision;

// Franjas horarias de retiro del colegio del supervisor. Las reglas (solapes, prioridad de la clase)
// están en el dominio: FranjaRetiro y HorarioDeRetiro.

public sealed class ListarFranjasUseCase(
    ContextoSupervisor contexto, IRepositorioFranjas franjas, IRepositorioClases clases, TimeProvider reloj) : IListarFranjasUseCase
{
    public const string AmbitoColegio = "Todo el colegio";

    public async Task<FranjasDelColegioDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var delColegio = await franjas.ListarPorColegioAsync(colegio.Id, cancellationToken);
        var opciones = (await clases.ListarPorColegioAsync(colegio.Id, cancellationToken))
            .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => new OpcionDto(c.Id, c.Nombre))
            .ToList();
        var nombres = opciones.ToDictionary(c => c.Id, c => c.Nombre);

        var lista = delColegio
            .Select(f => new FranjaDto(
                f.Id, f.ClaseId, f.ClaseId is { } claseId ? nombres.GetValueOrDefault(claseId, "—") : AmbitoColegio,
                f.DiaSemana, f.HoraInicio, f.HoraFin))
            .OrderBy(f => DiasSemana.Orden(f.DiaSemana))
            .ThenBy(f => f.HoraInicio)
            .ThenBy(f => f.ClaseId is null ? 0 : 1)
            .ThenBy(f => f.Ambito, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var ahora = colegio.HoraLocal(reloj.GetUtcNow());
        return new FranjasDelColegioDto(lista, opciones, colegio.ZonaHoraria, ahora, new HorarioDeRetiro(delColegio).ColegioEnFranja(ahora));
    }
}

public sealed class CrearFranjasUseCase(ContextoSupervisor contexto, IRepositorioFranjas franjas, IRepositorioClases clases) : ICrearFranjasUseCase
{
    public async Task<int> EjecutarAsync(string usuarioId, DatosFranjasDto datos, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        if (datos.ClaseId is { } claseId)
            await ContextoSupervisor.ClaseDelColegioAsync(clases, colegio.Id, claseId, cancellationToken);
        if (datos.Dias.Count == 0)
            throw new DominioException("Elige al menos un día de la semana.");

        var nuevas = datos.Dias.Distinct()
            .Select(dia => FranjaRetiro.Crear(colegio.Id, datos.ClaseId, dia, datos.Inicio, datos.Fin))
            .ToList();

        // Se comprueban todas antes de guardar: o se crean todas o ninguna.
        var existentes = await franjas.ListarPorColegioAsync(colegio.Id, cancellationToken);
        var choque = nuevas.FirstOrDefault(n => existentes.Any(n.SeSolapaCon));
        if (choque is not null)
        {
            var existente = existentes.First(choque.SeSolapaCon);
            throw new DominioException(
                $"El {DiasSemana.Nombre(choque.DiaSemana).ToLowerInvariant()} ya hay una franja de " +
                $"{existente.HoraInicio:HH\\:mm} a {existente.HoraFin:HH\\:mm} que se solapa con la nueva.");
        }

        await franjas.AgregarVariasAsync(nuevas, cancellationToken);
        return nuevas.Count;
    }
}

public sealed class EliminarFranjaUseCase(ContextoSupervisor contexto, IRepositorioFranjas franjas) : IEliminarFranjaUseCase
{
    public async Task EjecutarAsync(string usuarioId, Guid franjaId, CancellationToken cancellationToken)
    {
        var colegio = await contexto.ColegioDeAsync(usuarioId, cancellationToken);
        var franja = await franjas.ObtenerAsync(franjaId, cancellationToken);
        if (franja is null || franja.ColegioId != colegio.Id)
            throw new NoEncontradoException("La franja no existe.");
        await franjas.EliminarAsync(franja, cancellationToken);
    }
}
