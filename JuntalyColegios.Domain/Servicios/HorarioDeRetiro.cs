using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Domain.Servicios;

/// <summary>
/// Decide si a una hora local dada se llama a los alumnos de una clase. Reglas:
/// <list type="bullet">
///   <item>Si la clase tiene franjas propias ese día de la semana, solo cuentan esas.</item>
///   <item>Si no, cuentan las franjas generales del colegio de ese día.</item>
///   <item>Sin ninguna franja aplicable no se llama a nadie: un tag en la zona fuera de horario no
///   significa que vengan a recoger.</item>
/// </list>
/// </summary>
public sealed class HorarioDeRetiro(IEnumerable<FranjaRetiro> franjasDelColegio)
{
    private readonly IReadOnlyList<FranjaRetiro> _franjas = franjasDelColegio.ToList();

    public bool EstaEnFranja(Guid claseId, DateTime horaLocal)
    {
        var dia = horaLocal.DayOfWeek;
        var hora = TimeOnly.FromDateTime(horaLocal);

        var deLaClase = _franjas.Where(f => f.ClaseId == claseId && f.DiaSemana == dia).ToList();
        var aplicables = deLaClase.Count > 0
            ? deLaClase
            : _franjas.Where(f => f.ClaseId is null && f.DiaSemana == dia);

        return aplicables.Any(f => f.Contiene(dia, hora));
    }

    /// <summary>Si hay franja general del colegio a esa hora (aplica a las clases sin franja propia ese día).</summary>
    public bool ColegioEnFranja(DateTime horaLocal) =>
        _franjas.Any(f => f.ClaseId is null && f.Contiene(horaLocal.DayOfWeek, TimeOnly.FromDateTime(horaLocal)));
}
