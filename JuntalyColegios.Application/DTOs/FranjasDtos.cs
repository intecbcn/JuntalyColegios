namespace JuntalyColegios.Application.DTOs;

/// <summary>Alta de franjas: la misma hora para uno o varios días; sin clase aplica a todo el colegio.</summary>
public sealed record DatosFranjasDto(Guid? ClaseId, IReadOnlyList<DayOfWeek> Dias, TimeOnly Inicio, TimeOnly Fin);

/// <param name="Ambito">"Todo el colegio" o el nombre de la clase.</param>
public sealed record FranjaDto(Guid Id, Guid? ClaseId, string Ambito, DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

public sealed record OpcionDto(Guid Id, string Nombre);

/// <param name="HoraLocalActual">Hora actual en la zona horaria del colegio.</param>
/// <param name="ColegioEnFranjaAhora">Si ahora mismo hay franja general (se llamaría a los alumnos sin franja propia).</param>
public sealed record FranjasDelColegioDto(
    IReadOnlyList<FranjaDto> Franjas,
    IReadOnlyList<OpcionDto> Clases,
    string ZonaHoraria,
    DateTime HoraLocalActual,
    bool ColegioEnFranjaAhora);
