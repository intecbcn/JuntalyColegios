using System.ComponentModel.DataAnnotations;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Models;

namespace JuntalyColegios.Areas.PanelPadres.Models;

public sealed class AutorizadoFormulario
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(PersonaAutorizada.LongitudMaximaNombre, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    [Display(Name = "Nombre de la persona")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio.")]
    [StringLength(PersonaAutorizada.LongitudMaximaDocumento, ErrorMessage = "El documento no puede superar los {1} caracteres.")]
    [Display(Name = "Documento")]
    public string Documento { get; set; } = string.Empty;

    [StringLength(PersonaAutorizada.LongitudMaximaParentesco, ErrorMessage = "El parentesco no puede superar los {1} caracteres.")]
    [Display(Name = "Parentesco")]
    public string? Parentesco { get; set; }

    public DatosAutorizadoDto ADatos() => new(Nombre, Documento, Parentesco);
}

public sealed class PanelPadreViewModel
{
    public required PanelPadreDto Panel { get; init; }

    public VehiculoFormulario NuevoVehiculo { get; init; } = new();

    public AutorizadoFormulario NuevoAutorizado { get; init; } = new();
}
