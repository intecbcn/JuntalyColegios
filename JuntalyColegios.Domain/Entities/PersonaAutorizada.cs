using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Persona que un representante autoriza a recoger a sus hijos (abuela, chófer...). El personal la
/// identifica por su documento en la salida.
/// </summary>
public sealed class PersonaAutorizada
{
    public const int LongitudMaximaNombre = 100;
    public const int LongitudMinimaDocumento = 4;
    public const int LongitudMaximaDocumento = 30;
    public const int LongitudMaximaParentesco = 50;

    private PersonaAutorizada()
    {
    }

    public Guid Id { get; private set; }

    public Guid RepresentanteId { get; private set; }

    public Guid ColegioId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Documento de identidad (cédula, pasaporte), en mayúsculas.</summary>
    public string Documento { get; private set; } = string.Empty;

    public string? Parentesco { get; private set; }

    public static PersonaAutorizada Crear(Representante representante, string nombre, string documento, string? parentesco) => new()
    {
        Id = Guid.NewGuid(),
        RepresentanteId = representante.Id,
        ColegioId = representante.ColegioId,
        Nombre = Validacion.TextoObligatorio(nombre, "nombre", LongitudMaximaNombre),
        Documento = NormalizarDocumento(documento),
        Parentesco = Validacion.TextoOpcional(parentesco, "parentesco", LongitudMaximaParentesco)
    };

    public bool TieneDocumento(string documento) =>
        string.Equals(Documento, documento?.Trim().ToUpperInvariant(), StringComparison.Ordinal);

    private static string NormalizarDocumento(string? documento)
    {
        var limpio = documento?.Trim().ToUpperInvariant() ?? string.Empty;
        if (limpio.Length < LongitudMinimaDocumento || limpio.Length > LongitudMaximaDocumento
            || !limpio.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.'))
            throw new DominioException(
                $"El documento debe tener entre {LongitudMinimaDocumento} y {LongitudMaximaDocumento} letras, números, puntos o guiones.");
        return limpio;
    }
}
