using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>Clase o sección de un colegio (p.ej. "3.º A"). La gestiona su supervisor.</summary>
public sealed class Clase
{
    public const int LongitudMaximaNombre = 50;
    public const int LongitudMaximaNivel = 50;

    private Clase()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Nivel educativo opcional (p.ej. "Primaria"), útil para agrupar y para las franjas de retiro.</summary>
    public string? Nivel { get; private set; }

    public static Clase Crear(Guid colegioId, string nombre, string? nivel)
    {
        if (colegioId == Guid.Empty)
            throw new DominioException("La clase debe pertenecer a un colegio.");

        var clase = new Clase { Id = Guid.NewGuid(), ColegioId = colegioId };
        clase.Actualizar(nombre, nivel);
        return clase;
    }

    public void Actualizar(string nombre, string? nivel)
    {
        var nombreValido = Validacion.TextoObligatorio(nombre, "nombre", LongitudMaximaNombre);
        var nivelValido = Validacion.TextoOpcional(nivel, "nivel", LongitudMaximaNivel);
        Nombre = nombreValido;
        Nivel = nivelValido;
    }

    /// <summary>Compara nombres de clase como los percibe una persona (sin mayúsculas ni espacios sobrantes).</summary>
    public bool TieneNombre(string nombre) =>
        string.Equals(Nombre, nombre?.Trim(), StringComparison.CurrentCultureIgnoreCase);
}
