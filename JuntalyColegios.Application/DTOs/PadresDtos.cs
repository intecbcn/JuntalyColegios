namespace JuntalyColegios.Application.DTOs;

/// <summary>Alta de un padre o representante por el supervisor, con los hijos a vincular.</summary>
public sealed record AltaRepresentanteDto(string Nombre, string Email, string? Telefono, IReadOnlyList<Guid> AlumnoIds);

public sealed record RepresentanteResumenDto(Guid Id, string Nombre, string Email, string? Telefono, int NumeroHijos, int NumeroVehiculos);

public sealed record HijoDto(Guid AlumnoId, string NombreCompleto, string Clase);

public sealed record VehiculoDto(Guid Id, string Placa, string? Descripcion, string? Tag);

public sealed record AlumnoParaVincularDto(Guid AlumnoId, string NombreCompleto, string Clase);

public sealed record RepresentanteDetalleDto(
    Guid Id,
    string Nombre,
    string Email,
    string? Telefono,
    IReadOnlyList<HijoDto> Hijos,
    IReadOnlyList<VehiculoDto> Vehiculos,
    IReadOnlyList<AlumnoParaVincularDto> AlumnosParaVincular);

public sealed record DatosVehiculoDto(string Placa, string? Descripcion);
