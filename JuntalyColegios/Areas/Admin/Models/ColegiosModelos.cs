using System.ComponentModel.DataAnnotations;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Areas.Admin.Models;

/// <summary>Formulario de alta/edición de colegio. Las reglas de negocio las valida el dominio.</summary>
public sealed class ColegioFormulario
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(Colegio.LongitudMaximaNombre, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(Colegio.LongitudMaximaDireccion, ErrorMessage = "La dirección no puede superar los {1} caracteres.")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Display(Name = "Zona horaria")]
    public string? ZonaHoraria { get; set; } = Colegio.ZonaHorariaPorDefecto;

    /// <summary>Zonas horarias ofrecidas en el formulario (IANA), empezando por la del público objetivo.</summary>
    public static readonly IReadOnlyList<string> ZonasHorarias =
    [
        "America/Caracas", "America/Bogota", "America/Lima", "America/Guayaquil", "America/Panama",
        "America/Santo_Domingo", "America/Mexico_City", "America/Santiago", "America/Argentina/Buenos_Aires",
        "Europe/Madrid"
    ];

    public static ColegioFormulario Desde(ColegioDto colegio) => new()
    {
        Nombre = colegio.Nombre,
        Direccion = colegio.Direccion,
        ZonaHoraria = colegio.ZonaHoraria
    };

    public DatosColegioDto ADatos() => new(Nombre, Direccion, ZonaHoraria);
}

public sealed class SupervisorFormulario
{
    [Required(ErrorMessage = "El nombre del supervisor es obligatorio.")]
    [StringLength(Supervisor.LongitudMaximaNombre, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    [Display(Name = "Nombre del supervisor")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email del supervisor es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [Display(Name = "Email del supervisor")]
    public string Email { get; set; } = string.Empty;
}

public sealed class DetalleColegioViewModel
{
    public required ColegioDetalleDto Detalle { get; init; }

    public SupervisorFormulario NuevoSupervisor { get; init; } = new();
}
