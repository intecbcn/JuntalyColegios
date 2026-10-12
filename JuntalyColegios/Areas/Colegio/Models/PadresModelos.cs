using System.ComponentModel.DataAnnotations;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Models;

namespace JuntalyColegios.Areas.ColegioSupervisor.Models;

public sealed class RepresentanteFormulario
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(Representante.LongitudMaximaNombre, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(Representante.LongitudMaximaTelefono, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Hijos en el colegio")]
    public List<Guid> AlumnoIds { get; set; } = [];

    public AltaRepresentanteDto ADto() => new(Nombre, Email, Telefono, AlumnoIds);
}

public sealed class CrearRepresentanteViewModel
{
    public required RepresentanteFormulario Formulario { get; init; }

    public required IReadOnlyList<AlumnoParaVincularDto> Alumnos { get; init; }
}

public sealed class DetalleRepresentanteViewModel
{
    public required RepresentanteDetalleDto Detalle { get; init; }

    public VehiculoFormulario NuevoVehiculo { get; init; } = new();
}
