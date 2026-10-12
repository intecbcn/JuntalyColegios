using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

// Puertos de entrada del supervisor para las franjas horarias de retiro de su colegio.

public interface IListarFranjasUseCase
{
    Task<FranjasDelColegioDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

public interface ICrearFranjasUseCase
{
    /// <summary>Crea una franja por día indicado; si alguna no es válida no se crea ninguna. Devuelve cuántas creó.</summary>
    Task<int> EjecutarAsync(string usuarioId, DatosFranjasDto datos, CancellationToken cancellationToken);
}

public interface IEliminarFranjaUseCase
{
    Task EjecutarAsync(string usuarioId, Guid franjaId, CancellationToken cancellationToken);
}
