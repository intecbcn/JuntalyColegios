using System.Net.Mail;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.ValueObjects;

/// <summary>Dirección de email válida, normalizada en minúsculas y sin espacios.</summary>
public sealed record Email
{
    public const int LongitudMaxima = 256;

    private Email(string valor) => Valor = valor;

    public string Valor { get; }

    public static Email Crear(string? valor)
    {
        var limpio = valor?.Trim().ToLowerInvariant() ?? string.Empty;

        // MailAddress acepta formatos con nombre ("Ana <ana@x.com>"): se exige que la dirección sea
        // exactamente lo escrito, con dominio, y dentro del máximo que admite Identity.
        if (limpio.Length is 0 or > LongitudMaxima
            || !MailAddress.TryCreate(limpio, out var direccion)
            || direccion.Address != limpio
            || !direccion.Host.Contains('.'))
        {
            throw new DominioException("El email no es válido.");
        }

        return new Email(limpio);
    }

    public override string ToString() => Valor;
}
