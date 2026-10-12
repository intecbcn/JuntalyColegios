using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Franja horaria de retiro de un día de la semana. Solo dentro de una franja se llama a los alumnos
/// cuando la antena detecta un vehículo: fuera de ella el tag puede estar en la zona por otros motivos.
/// Sin clase aplica a todo el colegio; con clase, solo a esa clase (y prevalece ese día).
/// </summary>
public sealed class FranjaRetiro
{
    private FranjaRetiro()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    /// <summary>Null: franja general del colegio.</summary>
    public Guid? ClaseId { get; private set; }

    public DayOfWeek DiaSemana { get; private set; }

    /// <summary>Hora local del colegio (ver <see cref="Colegio.HoraLocal"/>).</summary>
    public TimeOnly HoraInicio { get; private set; }

    public TimeOnly HoraFin { get; private set; }

    public static FranjaRetiro Crear(Guid colegioId, Guid? claseId, DayOfWeek dia, TimeOnly inicio, TimeOnly fin)
    {
        if (colegioId == Guid.Empty)
            throw new DominioException("La franja debe pertenecer a un colegio.");
        if (!Enum.IsDefined(dia))
            throw new DominioException("El día de la semana no es válido.");
        if (inicio >= fin)
            throw new DominioException("La hora de inicio debe ser anterior a la hora de fin.");

        return new FranjaRetiro
        {
            Id = Guid.NewGuid(),
            ColegioId = colegioId,
            ClaseId = claseId,
            DiaSemana = dia,
            HoraInicio = inicio,
            HoraFin = fin
        };
    }

    /// <summary>Límites incluidos: una franja de 12:00 a 13:00 cubre las 12:00 y las 13:00.</summary>
    public bool Contiene(DayOfWeek dia, TimeOnly hora) => dia == DiaSemana && hora >= HoraInicio && hora <= HoraFin;

    /// <summary>
    /// Mismo colegio, mismo ámbito (colegio o la misma clase) y mismo día con horas que se cruzan. Dos
    /// franjas consecutivas (12:00–13:00 y 13:00–14:00) no se consideran solapadas.
    /// </summary>
    public bool SeSolapaCon(FranjaRetiro otra) =>
        ColegioId == otra.ColegioId
        && ClaseId == otra.ClaseId
        && DiaSemana == otra.DiaSemana
        && HoraInicio < otra.HoraFin
        && otra.HoraInicio < HoraFin;
}
