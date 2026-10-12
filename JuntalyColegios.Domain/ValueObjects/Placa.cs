using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.ValueObjects;

/// <summary>Placa de un vehículo, en mayúsculas y sin espacios (se admiten guiones).</summary>
public sealed record Placa
{
    public const int LongitudMinima = 4;
    public const int LongitudMaxima = 10;

    private Placa(string valor) => Valor = valor;

    public string Valor { get; }

    public static Placa Crear(string? valor)
    {
        var limpio = new string((valor ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (limpio.Length < LongitudMinima || limpio.Length > LongitudMaxima
            || !limpio.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            throw new DominioException(
                $"La placa debe tener entre {LongitudMinima} y {LongitudMaxima} letras, números o guiones.");
        return new Placa(limpio);
    }

    public override string ToString() => Valor;
}
