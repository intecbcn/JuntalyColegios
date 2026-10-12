using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.DTOs;

/// <summary>Resultado de procesar una lectura de tag: a quién se llamó (si a alguien) y por qué.</summary>
public sealed record ResultadoDeteccionDto(ResultadoDeteccion Resultado, string Tag, string? Placa, IReadOnlyList<string> AlumnosLlamados);

public sealed record RecogidaPendienteDto(
    Guid Id,
    string Alumno,
    string Clase,
    string Placa,
    string? Vehiculo,
    string Representante,
    DateTime DetectadoEnLocal,
    IReadOnlyList<PersonaAutorizadaDto> Autorizados);

public sealed record VehiculoParaSimularDto(Guid Id, string Placa, string Representante);

public sealed record PantallaSalidaDto(
    string NombreColegio,
    DateTime HoraLocal,
    IReadOnlyList<RecogidaPendienteDto> Pendientes,
    IReadOnlyList<VehiculoParaSimularDto> VehiculosParaSimular);
