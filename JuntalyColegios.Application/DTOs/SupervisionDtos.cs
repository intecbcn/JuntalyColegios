namespace JuntalyColegios.Application.DTOs;

/// <summary>Datos editables de una clase.</summary>
public sealed record DatosClaseDto(string Nombre, string? Nivel);

public sealed record ClaseResumenDto(Guid Id, string Nombre, string? Nivel, int NumeroAlumnos);

public sealed record ClasesDelColegioDto(string NombreColegio, IReadOnlyList<ClaseResumenDto> Clases);

public sealed record AlumnoDto(Guid Id, Guid ClaseId, string Nombres, string Apellidos, bool Activo);

public sealed record ClaseDetalleDto(Guid Id, string Nombre, string? Nivel, IReadOnlyList<AlumnoDto> Alumnos);

/// <summary>Datos editables de un alumno, incluida la clase a la que pertenece.</summary>
public sealed record DatosAlumnoDto(Guid ClaseId, string Nombres, string Apellidos);
