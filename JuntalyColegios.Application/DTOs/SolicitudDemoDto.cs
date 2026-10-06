namespace JuntalyColegios.Application.DTOs;

/// <summary>Datos del formulario "Solicita una demo" de la landing.</summary>
public sealed record SolicitudDemoDto(
    string Nombre,
    string Colegio,
    string Contacto,
    int? NumeroAlumnos);
