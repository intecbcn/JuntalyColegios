namespace JuntalyColegios.Domain.Entities;

/// <summary>Qué provocó una lectura de la antena.</summary>
public enum ResultadoDeteccion
{
    /// <summary>Se llamó a uno o más alumnos.</summary>
    GeneraRecogida = 1,

    /// <summary>Vehículo conocido fuera de la franja de retiro: el tag estaba en la zona por otro motivo.</summary>
    FueraDeFranja = 2,

    /// <summary>Ningún vehículo del colegio tiene ese tag.</summary>
    TagDesconocido = 3,

    /// <summary>El vehículo ya se había detectado hace poco o sus hijos ya están llamados.</summary>
    Duplicada = 4,

    /// <summary>El vehículo no tiene hijos activos vinculados.</summary>
    SinAlumnos = 5
}

/// <summary>
/// Lectura de un tag por la antena del colegio (o simulada). Se registra siempre, genere o no llamadas,
/// para poder revisar qué pasó a la salida.
/// </summary>
public sealed class Deteccion
{
    private Deteccion()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    public string Tag { get; private set; } = string.Empty;

    /// <summary>Vehículo reconocido; null si el tag es desconocido.</summary>
    public Guid? VehiculoId { get; private set; }

    public DateTimeOffset DetectadoEn { get; private set; }

    public ResultadoDeteccion Resultado { get; private set; }

    /// <summary>Generada desde el simulador de la pantalla de salida, no por la antena.</summary>
    public bool Simulada { get; private set; }

    public static Deteccion Crear(
        Guid colegioId, string tag, Guid? vehiculoId, DateTimeOffset detectadoEn, ResultadoDeteccion resultado, bool simulada) => new()
    {
        Id = Guid.NewGuid(),
        ColegioId = colegioId,
        Tag = tag,
        VehiculoId = vehiculoId,
        DetectadoEn = detectadoEn,
        Resultado = resultado,
        Simulada = simulada
    };
}
