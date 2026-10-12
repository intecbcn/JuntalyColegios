using System.ComponentModel.DataAnnotations;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Areas.ColegioSupervisor.Models;

public sealed class ClaseFormulario
{
    [Required(ErrorMessage = "El nombre de la clase es obligatorio.")]
    [StringLength(Clase.LongitudMaximaNombre, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    [Display(Name = "Nombre de la clase")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(Clase.LongitudMaximaNivel, ErrorMessage = "El nivel no puede superar los {1} caracteres.")]
    [Display(Name = "Nivel")]
    public string? Nivel { get; set; }

    public DatosClaseDto ADatos() => new(Nombre, Nivel);
}

public sealed class AlumnoFormulario
{
    [Display(Name = "Clase")]
    public Guid ClaseId { get; set; }

    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(Alumno.LongitudMaximaNombres, ErrorMessage = "Los nombres no pueden superar los {1} caracteres.")]
    [Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(Alumno.LongitudMaximaApellidos, ErrorMessage = "Los apellidos no pueden superar los {1} caracteres.")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    public DatosAlumnoDto ADatos() => new(ClaseId, Nombres, Apellidos);
}

public sealed class DetalleClaseViewModel
{
    public required ClaseDetalleDto Detalle { get; init; }

    public required AlumnoFormulario NuevoAlumno { get; init; }
}

public sealed class EditarAlumnoViewModel
{
    public required Guid AlumnoId { get; init; }

    public required bool Activo { get; init; }

    public required AlumnoFormulario Alumno { get; init; }

    public required IReadOnlyList<ClaseResumenDto> Clases { get; init; }
}
