namespace JuntalyColegios.Application.Formato;

/// <summary>Días de la semana en español y en el orden de una semana escolar (de lunes a domingo).</summary>
public static class DiasSemana
{
    public static readonly IReadOnlyList<DayOfWeek> DeLunesADomingo =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public static string Nombre(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "Lunes",
        DayOfWeek.Tuesday => "Martes",
        DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves",
        DayOfWeek.Friday => "Viernes",
        DayOfWeek.Saturday => "Sábado",
        DayOfWeek.Sunday => "Domingo",
        _ => dia.ToString()
    };

    /// <summary>Posición del día empezando por el lunes (0) para ordenar.</summary>
    public static int Orden(DayOfWeek dia) => ((int)dia + 6) % 7;
}
