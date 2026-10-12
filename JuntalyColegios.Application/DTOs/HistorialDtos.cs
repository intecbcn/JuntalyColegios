using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.DTOs;

/// <summary>Una recogida en el historial del padre (horas en la zona horaria del colegio).</summary>
public sealed record HistorialRecogidaDto(string Alumno, string? Placa, DateTime LlegadaLocal, DateTime? EntregaLocal);

/// <summary>Una lectura en el registro de detecciones del supervisor.</summary>
public sealed record DeteccionRegistroDto(
    DateTime DetectadoEnLocal, string Tag, string? Placa, string? Familia, ResultadoDeteccion Resultado, bool Simulada);
