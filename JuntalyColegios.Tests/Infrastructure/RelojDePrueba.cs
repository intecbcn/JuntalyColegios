namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>
/// Reloj controlable para los tests. A diferencia de FakeTimeProvider permite volver atrás, para que
/// cada test pueda fijar la hora que necesita aunque otro test anterior la haya adelantado.
/// </summary>
public sealed class RelojDePrueba(DateTimeOffset ahora) : TimeProvider
{
    private DateTimeOffset _ahora = ahora;

    public override DateTimeOffset GetUtcNow() => _ahora;

    public void SetUtcNow(DateTimeOffset ahora) => _ahora = ahora;

    public void Advance(TimeSpan intervalo) => _ahora += intervalo;
}
