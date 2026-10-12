using JuntalyColegios.Application.DTOs;

namespace JuntalyColegios.Application.Ports.In;

public interface IListarPantallasUseCase
{
    Task<PantallasDelColegioDto> EjecutarAsync(string usuarioId, CancellationToken cancellationToken);
}

/// <summary>Crea la pantalla (portería si no hay clase) o regenera su enlace; devuelve el token una única vez.</summary>
public interface ICrearPantallaUseCase
{
    Task<string> EjecutarAsync(string usuarioId, Guid? claseId, CancellationToken cancellationToken);
}

public interface IRevocarPantallaUseCase
{
    Task EjecutarAsync(string usuarioId, Guid pantallaId, CancellationToken cancellationToken);
}

/// <summary>Contenido de una pantalla a partir de su enlace secreto. Lanza AccesoDenegadoException si no es válido.</summary>
public interface IObtenerPantallaPublicaUseCase
{
    Task<PantallaPublicaDto> EjecutarAsync(string? token, CancellationToken cancellationToken);
}
