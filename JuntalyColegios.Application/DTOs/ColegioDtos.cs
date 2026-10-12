namespace JuntalyColegios.Application.DTOs;

/// <summary>Datos editables de un colegio (alta y edición).</summary>
public sealed record DatosColegioDto(string Nombre, string? Direccion, string? ZonaHoraria);

public sealed record ColegioDto(Guid Id, string Nombre, string? Direccion, string ZonaHoraria, bool Activo, bool TieneClaveAntena = false);

public sealed record SupervisorDto(Guid Id, string Nombre, string Email);

public sealed record ColegioDetalleDto(ColegioDto Colegio, IReadOnlyList<SupervisorDto> Supervisores);

public sealed record AltaSupervisorDto(Guid ColegioId, string Nombre, string Email);

public sealed record DatosSupervisorDto(string Nombre, string Email);
