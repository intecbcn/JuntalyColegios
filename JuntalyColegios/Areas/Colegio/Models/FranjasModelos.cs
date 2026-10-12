using System.ComponentModel.DataAnnotations;
using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Areas.ColegioSupervisor.Models;

public sealed class FranjaFormulario
{
    /// <summary>Vacío: franja para todo el colegio.</summary>
    [Display(Name = "Aplica a")]
    public Guid? ClaseId { get; set; }

    [Display(Name = "Días")]
    public List<DayOfWeek> Dias { get; set; } = [];

    [Required(ErrorMessage = "Indica la hora de inicio.")]
    [Display(Name = "Desde")]
    public TimeOnly? HoraInicio { get; set; }

    [Required(ErrorMessage = "Indica la hora de fin.")]
    [Display(Name = "Hasta")]
    public TimeOnly? HoraFin { get; set; }

    public DatosFranjasDto ADatos() => new(ClaseId, Dias, HoraInicio!.Value, HoraFin!.Value);
}
