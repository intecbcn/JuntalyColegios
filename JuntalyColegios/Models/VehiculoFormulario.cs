using System.ComponentModel.DataAnnotations;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Models;

/// <summary>Formulario de vehículo (placa y descripción), compartido por el supervisor y el panel de padres.</summary>
public sealed class VehiculoFormulario
{
    [Required(ErrorMessage = "La placa es obligatoria.")]
    [StringLength(Domain.ValueObjects.Placa.LongitudMaxima + 4, ErrorMessage = "La placa es demasiado larga.")]
    [Display(Name = "Placa")]
    public string Placa { get; set; } = string.Empty;

    [StringLength(Vehiculo.LongitudMaximaDescripcion, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    public DatosVehiculoDto ADatos() => new(Placa, Descripcion);
}
