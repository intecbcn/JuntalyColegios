using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Application.UseCases.Colegios;

// Casos de uso de la gestión de colegios (área Admin). Uno por operación de negocio; las reglas viven
// en las entidades del dominio y aquí solo se orquesta.

internal static class ColegiosMapeo
{
    public static ColegioDto ADto(this Colegio c) => new(c.Id, c.Nombre, c.Direccion, c.ZonaHoraria, c.Activo, c.HashClaveAntena is not null);

    public static async Task<Colegio> ObtenerExistenteAsync(this IRepositorioColegios colegios, Guid id, CancellationToken ct) =>
        await colegios.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("El colegio no existe.");

    /// <summary>El supervisor solo existe para el colegio al que pertenece.</summary>
    public static async Task<Supervisor> SupervisorDelColegioAsync(
        this IRepositorioSupervisores supervisores, Guid colegioId, Guid supervisorId, CancellationToken ct)
    {
        var supervisor = await supervisores.ObtenerAsync(supervisorId, ct);
        return supervisor is not null && supervisor.ColegioId == colegioId
            ? supervisor
            : throw new NoEncontradoException("El supervisor no existe.");
    }
}

public sealed class CrearColegioUseCase(IRepositorioColegios colegios) : ICrearColegioUseCase
{
    public async Task<Guid> EjecutarAsync(DatosColegioDto datos, CancellationToken cancellationToken)
    {
        var colegio = Colegio.Crear(datos.Nombre, datos.Direccion, datos.ZonaHoraria);
        await colegios.AgregarAsync(colegio, cancellationToken);
        return colegio.Id;
    }
}

public sealed class ListarColegiosUseCase(IRepositorioColegios colegios) : IListarColegiosUseCase
{
    public async Task<IReadOnlyList<ColegioDto>> EjecutarAsync(CancellationToken cancellationToken) =>
        (await colegios.ListarAsync(cancellationToken))
            .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => c.ADto())
            .ToList();
}

public sealed class ObtenerColegioUseCase(IRepositorioColegios colegios, IRepositorioSupervisores supervisores) : IObtenerColegioUseCase
{
    public async Task<ColegioDetalleDto> EjecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var colegio = await colegios.ObtenerExistenteAsync(id, cancellationToken);
        var deColegio = await supervisores.ListarPorColegioAsync(id, cancellationToken);
        return new ColegioDetalleDto(
            colegio.ADto(),
            deColegio.OrderBy(s => s.Nombre).Select(s => new SupervisorDto(s.Id, s.Nombre, s.Email)).ToList());
    }
}

public sealed class EditarColegioUseCase(IRepositorioColegios colegios) : IEditarColegioUseCase
{
    public async Task EjecutarAsync(Guid id, DatosColegioDto datos, CancellationToken cancellationToken)
    {
        var colegio = await colegios.ObtenerExistenteAsync(id, cancellationToken);
        colegio.Actualizar(datos.Nombre, datos.Direccion, datos.ZonaHoraria);
        await colegios.ActualizarAsync(colegio, cancellationToken);
    }
}

public sealed class CambiarEstadoColegioUseCase(IRepositorioColegios colegios) : ICambiarEstadoColegioUseCase
{
    public async Task EjecutarAsync(Guid id, bool activo, CancellationToken cancellationToken)
    {
        var colegio = await colegios.ObtenerExistenteAsync(id, cancellationToken);
        if (activo) colegio.Activar();
        else colegio.Desactivar();
        await colegios.ActualizarAsync(colegio, cancellationToken);
    }
}

public sealed class AltaSupervisorUseCase(
    IRepositorioColegios colegios, IRepositorioSupervisores supervisores, IServicioUsuarios usuarios) : IAltaSupervisorUseCase
{
    public async Task<Guid> EjecutarAsync(AltaSupervisorDto alta, CancellationToken cancellationToken)
    {
        var colegio = await colegios.ObtenerExistenteAsync(alta.ColegioId, cancellationToken);
        if (!colegio.Activo)
            throw new DominioException("No se pueden dar de alta supervisores en un colegio desactivado.");

        // Todo se valida antes de crear la cuenta, para no dejar cuentas huérfanas por datos inválidos.
        var nombre = Supervisor.ValidarNombre(alta.Nombre);
        var email = Email.Crear(alta.Email);

        var usuarioId = await usuarios.CrearUsuarioInvitadoAsync(email.Valor, nombre, Roles.SupervisorColegio, cancellationToken);
        var supervisor = Supervisor.Crear(colegio.Id, usuarioId, nombre, email);
        try
        {
            await supervisores.AgregarAsync(supervisor, cancellationToken);
        }
        catch
        {
            // Sin el vínculo con el colegio la cuenta quedaría huérfana y bloquearía reintentar el alta.
            await usuarios.EliminarUsuarioAsync(usuarioId, CancellationToken.None);
            throw;
        }
        return supervisor.Id;
    }
}

/// <summary>
/// Cambia nombre y email del supervisor. Si cambia el email también cambia el de su cuenta (con el que
/// inicia sesión); si otra cuenta ya lo usa no se cambia nada.
/// </summary>
public sealed class EditarSupervisorUseCase(IRepositorioSupervisores supervisores, IServicioUsuarios usuarios) : IEditarSupervisorUseCase
{
    public async Task EjecutarAsync(Guid colegioId, Guid supervisorId, DatosSupervisorDto datos, CancellationToken cancellationToken)
    {
        var supervisor = await supervisores.SupervisorDelColegioAsync(colegioId, supervisorId, cancellationToken);
        var nombre = Supervisor.ValidarNombre(datos.Nombre);
        var email = Email.Crear(datos.Email);

        if (email.Valor != supervisor.Email)
            await usuarios.CambiarEmailAsync(supervisor.UsuarioId, email.Valor, cancellationToken);

        supervisor.Actualizar(nombre, email);
        await supervisores.ActualizarAsync(supervisor, cancellationToken);
    }
}

/// <summary>Da de baja al supervisor y elimina su cuenta: deja de poder iniciar sesión.</summary>
public sealed class EliminarSupervisorUseCase(IRepositorioSupervisores supervisores, IServicioUsuarios usuarios) : IEliminarSupervisorUseCase
{
    public async Task EjecutarAsync(Guid colegioId, Guid supervisorId, CancellationToken cancellationToken)
    {
        var supervisor = await supervisores.SupervisorDelColegioAsync(colegioId, supervisorId, cancellationToken);
        await supervisores.EliminarAsync(supervisor, cancellationToken);
        await usuarios.EliminarUsuarioAsync(supervisor.UsuarioId, cancellationToken);
    }
}
