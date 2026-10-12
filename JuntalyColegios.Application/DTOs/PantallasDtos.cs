namespace JuntalyColegios.Application.DTOs;

/// <param name="Nombre">"Portería" o "Aula …".</param>
public sealed record PantallaDto(Guid Id, string Nombre, Guid? ClaseId);

public sealed record PantallasDelColegioDto(IReadOnlyList<PantallaDto> Pantallas, IReadOnlyList<OpcionDto> Clases);

/// <summary>Un alumno llamado, con lo mínimo para encontrarlo (sin datos de la familia).</summary>
public sealed record LlamadoDto(Guid RecogidaId, string Alumno, string Clase, string? Placa, DateTime LlamadoLocal);

public sealed record PantallaPublicaDto(
    string NombreColegio, string Titulo, DateTime HoraLocal, LlamadoDto? LlamandoAhora, IReadOnlyList<LlamadoDto> Anteriores);
