namespace JuntalyColegios.Models;

/// <summary>Cuerpo JSON que envía la antena: { "tag": "E200..." }.</summary>
public sealed class LecturaAntenaRequest
{
    public string Tag { get; set; } = string.Empty;
}
