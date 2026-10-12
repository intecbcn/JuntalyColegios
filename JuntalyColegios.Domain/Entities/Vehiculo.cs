using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Vehículo de un representante. Cuando tiene tag RFID, la antena del colegio lo reconoce a la salida
/// y se llama a los hijos de su representante.
/// </summary>
public sealed class Vehiculo
{
    public const int LongitudMaximaDescripcion = 100;

    private Vehiculo()
    {
    }

    public Guid Id { get; private set; }

    public Guid RepresentanteId { get; private set; }

    /// <summary>Colegio del representante: el tag es único dentro de cada colegio.</summary>
    public Guid ColegioId { get; private set; }

    public string Placa { get; private set; } = string.Empty;

    /// <summary>Marca, modelo o color, para que el personal lo reconozca en la fila.</summary>
    public string? Descripcion { get; private set; }

    public string? Tag { get; private set; }

    public static Vehiculo Crear(Representante representante, Placa placa, string? descripcion) => new()
    {
        Id = Guid.NewGuid(),
        RepresentanteId = representante.Id,
        ColegioId = representante.ColegioId,
        Placa = placa.Valor,
        Descripcion = Validacion.TextoOpcional(descripcion, "descripción", LongitudMaximaDescripcion)
    };

    public void AsignarTag(TagRfid tag) => Tag = tag.Valor;

    public void QuitarTag() => Tag = null;
}
