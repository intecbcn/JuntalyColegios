using JuntalyColegios.Domain.Entities;

namespace JuntalyColegios.Application.Formato;

/// <summary>Resultado de una detección tal como lo lee el personal del colegio.</summary>
public static class ResultadosDeteccion
{
    public static string Texto(ResultadoDeteccion resultado) => resultado switch
    {
        ResultadoDeteccion.GeneraRecogida => "Alumnos llamados",
        ResultadoDeteccion.FueraDeFranja => "Fuera de franja",
        ResultadoDeteccion.TagDesconocido => "Tag desconocido",
        ResultadoDeteccion.Duplicada => "Repetida",
        ResultadoDeteccion.SinAlumnos => "Sin alumnos activos",
        _ => resultado.ToString()
    };
}
