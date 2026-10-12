using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.ValueObjects;

/// <summary>
/// Identificador del tag RFID de un vehículo, tal como lo lee la antena (EPC en hexadecimal u otro
/// código alfanumérico). Se normaliza en mayúsculas para que la lectura y el registro coincidan.
/// </summary>
public sealed record TagRfid
{
    public const int LongitudMinima = 4;
    public const int LongitudMaxima = 32;

    private TagRfid(string valor) => Valor = valor;

    public string Valor { get; }

    public static TagRfid Crear(string? valor)
    {
        var limpio = valor?.Trim().ToUpperInvariant() ?? string.Empty;
        if (limpio.Length < LongitudMinima || limpio.Length > LongitudMaxima || !limpio.All(char.IsAsciiLetterOrDigit))
            throw new DominioException(
                $"El tag RFID debe tener entre {LongitudMinima} y {LongitudMaxima} letras o números, sin espacios ni símbolos.");
        return new TagRfid(limpio);
    }

    public override string ToString() => Valor;
}
