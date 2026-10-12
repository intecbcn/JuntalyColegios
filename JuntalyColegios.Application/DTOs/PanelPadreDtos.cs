namespace JuntalyColegios.Application.DTOs;

public sealed record PersonaAutorizadaDto(Guid Id, string Nombre, string Documento, string? Parentesco);

/// <summary>Todo lo que ve un padre de su familia: hijos con su clase, vehículos (con tag) y autorizados.</summary>
public sealed record PanelPadreDto(
    string NombrePadre,
    string NombreColegio,
    IReadOnlyList<HijoDto> Hijos,
    IReadOnlyList<VehiculoDto> Vehiculos,
    IReadOnlyList<PersonaAutorizadaDto> Autorizados);

public sealed record DatosAutorizadoDto(string Nombre, string Documento, string? Parentesco);
