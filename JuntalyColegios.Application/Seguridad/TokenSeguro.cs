using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace JuntalyColegios.Application.Seguridad;

/// <summary>
/// Secretos para identificar a quien no inicia sesión (antena, pantallas de llamada): 256 bits
/// aleatorios. Solo se guarda su hash SHA-256; al ser aleatorios y largos, el hash sirve también para
/// buscarlos sin exponerlos.
/// </summary>
public static class TokenSeguro
{
    public static string Generar() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
