using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain;

/// <summary>Validaciones de texto compartidas por las entidades del dominio.</summary>
internal static class Validacion
{
    /// <summary>Devuelve el texto sin espacios sobrantes; falla si está vacío o supera el máximo.</summary>
    public static string TextoObligatorio(string? valor, string campo, int longitudMaxima)
    {
        var limpio = valor?.Trim();
        if (string.IsNullOrEmpty(limpio))
            throw new DominioException($"El campo {campo} es obligatorio.");
        return LongitudMaxima(limpio, campo, longitudMaxima);
    }

    /// <summary>Devuelve el texto sin espacios sobrantes, o null si viene vacío; falla si supera el máximo.</summary>
    public static string? TextoOpcional(string? valor, string campo, int longitudMaxima)
    {
        var limpio = valor?.Trim();
        return string.IsNullOrEmpty(limpio) ? null : LongitudMaxima(limpio, campo, longitudMaxima);
    }

    private static string LongitudMaxima(string valor, string campo, int longitudMaxima) =>
        valor.Length <= longitudMaxima
            ? valor
            : throw new DominioException($"El campo {campo} no puede superar los {longitudMaxima} caracteres.");
}
